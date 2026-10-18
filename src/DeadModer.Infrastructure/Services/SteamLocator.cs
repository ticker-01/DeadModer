using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Exceptions;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace DeadModer.Infrastructure.Services;

/// <summary>
/// Реализация сервиса обнаружения библиотек Steam и директории игры Deadlock (AppID: 1422450).
/// Анализирует реестр Windows, файл libraryfolders.vdf с помощью Gameloop.Vdf и манифесты .acf.
/// </summary>
public sealed class SteamLocator : ISteamLocator
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ILogger<SteamLocator>? _logger;

    public SteamLocator(IAppSettingsRepository settingsRepository, ILogger<SteamLocator>? logger = null)
    {
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<string?> FindSteamInstallPathAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (OperatingSystem.IsWindows())
            {
                // 1. Проверяем HKCU (текущий пользователь)
                try
                {
                    using var cuKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                    var steamPath = cuKey?.GetValue("SteamPath")?.ToString();
                    if (!string.IsNullOrWhiteSpace(steamPath) && Directory.Exists(steamPath))
                    {
                        return NormalizePath(steamPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Ошибка чтения реестра HKCU\\Software\\Valve\\Steam");
                }

                // 2. Проверяем HKLM 64-bit / 32-bit
                try
                {
                    using var lmKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam") 
                                   ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");
                    var installPath = lmKey?.GetValue("InstallPath")?.ToString();
                    if (!string.IsNullOrWhiteSpace(installPath) && Directory.Exists(installPath))
                    {
                        return NormalizePath(installPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Ошибка чтения реестра HKLM Valve\\Steam");
                }
            }

            // 3. Стандартные резервные пути на дисках
            var candidatePaths = new List<string>
            {
                @"D:\Program Files\Steam",
                @"D:\Program Files (x86)\Steam",
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam",
                @"D:\Steam",
                @"D:\SteamLibrary",
                @"E:\Steam",
                @"E:\SteamLibrary"
            };

            try
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    candidatePaths.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam"));
                    candidatePaths.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam"));
                    candidatePaths.Add(Path.Combine(drive.RootDirectory.FullName, "Steam"));
                    candidatePaths.Add(Path.Combine(drive.RootDirectory.FullName, "SteamLibrary"));
                }
            }
            catch { }

            foreach (var path in candidatePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steam.exe")))
                {
                    return NormalizePath(path);
                }
            }

            _logger?.LogWarning("Директория клиента Steam не обнаружена в системе");
            return null;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAllSteamLibraryFoldersAsync(CancellationToken cancellationToken = default)
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var steamRoot = await FindSteamInstallPathAsync(cancellationToken);
        if (!string.IsNullOrEmpty(steamRoot))
        {
            libraries.Add(steamRoot);

            var vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                try
                {
                    var parsed = await ParseLibraryFoldersVdfAsync(vdfPath, cancellationToken);
                    foreach (var lib in parsed)
                    {
                        libraries.Add(lib);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка при парсинге {VdfPath}. Запуск аварийного парсера через Regex", vdfPath);
                    var fallbackLibs = ParseLibraryFoldersWithRegex(vdfPath);
                    foreach (var lib in fallbackLibs)
                    {
                        libraries.Add(lib);
                    }
                }
            }
        }

        return libraries.Where(Directory.Exists).ToList();
    }

    /// <inheritdoc />
    public async Task<string?> FindDeadlockGamePathAsync(CancellationToken cancellationToken = default)
    {
        // 1. Проверяем сохраненный пользовательский путь в AppSettings
        var settings = await _settingsRepository.GetSettingsAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(settings.GamePath) && IsValidDeadlockDirectory(settings.GamePath))
        {
            _logger?.LogDebug("Используется валидный путь Deadlock из настроек: {Path}", settings.GamePath);
            return NormalizePath(settings.GamePath);
        }

        // 2. Проверяем известные и распространенные пути к Deadlock на дисках
        var wellKnownDirs = new List<string>
        {
            @"D:\Program Files\Steam\steamapps\common\Deadlock",
            @"D:\Program Files (x86)\Steam\steamapps\common\Deadlock",
            @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock",
            @"C:\Program Files\Steam\steamapps\common\Deadlock",
            @"D:\SteamLibrary\steamapps\common\Deadlock",
            @"E:\SteamLibrary\steamapps\common\Deadlock"
        };

        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                wellKnownDirs.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam", "steamapps", "common", "Deadlock"));
                wellKnownDirs.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam", "steamapps", "common", "Deadlock"));
                wellKnownDirs.Add(Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "common", "Deadlock"));
                wellKnownDirs.Add(Path.Combine(drive.RootDirectory.FullName, "Steam", "steamapps", "common", "Deadlock"));
            }
        }
        catch { }

        foreach (var dir in wellKnownDirs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (IsValidDeadlockDirectory(dir))
            {
                var normalized = NormalizePath(dir);
                _logger?.LogInformation("Игра Deadlock успешно обнаружена по прямому пути: {Path}", normalized);
                settings.GamePath = normalized;
                await _settingsRepository.SaveSettingsAsync(settings, cancellationToken);
                return normalized;
            }
        }

        // 3. Сканируем все найденные библиотеки Steam
        var libraries = await GetAllSteamLibraryFoldersAsync(cancellationToken);
        var deadlockAppIdStr = ISteamLocator.DeadlockAppId.ToString();

        foreach (var library in libraries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var steamApps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamApps)) continue;

            // Проверка манифеста Deadlock: appmanifest_1422450.acf
            var manifestPath = Path.Combine(steamApps, $"appmanifest_{deadlockAppIdStr}.acf");
            var candidateGameDir = Path.Combine(steamApps, "common", "Deadlock");

            if (File.Exists(manifestPath) || Directory.Exists(candidateGameDir))
            {
                if (IsValidDeadlockDirectory(candidateGameDir))
                {
                    var normalized = NormalizePath(candidateGameDir);
                    _logger?.LogInformation("Игра Deadlock успешно обнаружена: {Path}", normalized);

                    // Автоматически сохраняем найденный путь в AppSettings
                    settings.GamePath = normalized;
                    await _settingsRepository.SaveSettingsAsync(settings, cancellationToken);

                    return normalized;
                }
            }
        }

        _logger?.LogWarning("Deadlock (AppID: {AppId}) не найден ни в одной библиотеке Steam", ISteamLocator.DeadlockAppId);
        return null;
    }

    /// <inheritdoc />
    public async Task<string> GetAddonsPathAsync(CancellationToken cancellationToken = default)
    {
        var gamePath = await FindDeadlockGamePathAsync(cancellationToken);
        if (string.IsNullOrEmpty(gamePath))
        {
            throw new GameNotFoundException();
        }

        // Структура Source 2: Deadlock/game/citadel/addons
        var addonsPath = Path.Combine(gamePath, "game", "citadel", "addons");

        try
        {
            if (!Directory.Exists(addonsPath))
            {
                Directory.CreateDirectory(addonsPath);
                _logger?.LogInformation("Создана папка модов Source 2: {AddonsPath}", addonsPath);
            }

            return NormalizePath(addonsPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Отказано в доступе при попытке создать или открыть директорию: {Path}", addonsPath);
            throw new UnauthorizedAccessException($"Недостаточно прав для создания папки аддонов '{addonsPath}'. Запустите приложение с правами администратора.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsGameInstalledAsync(CancellationToken cancellationToken = default)
    {
        var path = await FindDeadlockGamePathAsync(cancellationToken);
        return !string.IsNullOrEmpty(path);
    }

    /// <summary>
    /// Парсинг libraryfolders.vdf с использованием Gameloop.Vdf.
    /// </summary>
    private async Task<List<string>> ParseLibraryFoldersVdfAsync(string vdfPath, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        var content = await File.ReadAllTextAsync(vdfPath, cancellationToken);
        var volvo = VdfConvert.Deserialize(content);

        if (volvo.Value is VObject root)
        {
            foreach (var prop in root.Properties())
            {
                if (prop.Value is VObject folderObj)
                {
                    var pathToken = folderObj["path"];
                    if (pathToken is VValue val && val.Value != null)
                    {
                        var path = val.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            result.Add(NormalizePath(path));
                        }
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Резервный парсер libraryfolders.vdf на регулярных выражениях на случай нестандартных VDF-структур.
    /// </summary>
    private static List<string> ParseLibraryFoldersWithRegex(string vdfPath)
    {
        var result = new List<string>();
        try
        {
            var content = File.ReadAllText(vdfPath);
            // Шаблон поиска "path" "D:\\SteamLibrary"
            var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var raw = match.Groups[1].Value.Replace(@"\\", @"\");
                    if (!string.IsNullOrWhiteSpace(raw) && Directory.Exists(raw))
                    {
                        result.Add(NormalizePath(raw));
                    }
                }
            }
        }
        catch
        {
            // Игнорируем ошибки резервного парсера
        }

        return result;
    }

    /// <summary>
    /// Проверка, что директория действительно является директорией Deadlock Source 2.
    /// </summary>
    private static bool IsValidDeadlockDirectory(string gameDir)
    {
        if (!Directory.Exists(gameDir)) return false;

        // В папке Deadlock обязательно присутствует каталог game/citadel с файлом gameinfo.gi или citadel.exe / project8.exe
        var citadelDir = Path.Combine(gameDir, "game", "citadel");
        if (Directory.Exists(citadelDir))
        {
            return true;
        }

        return false;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
}
