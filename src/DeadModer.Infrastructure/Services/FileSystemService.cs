using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Entities;
using DeadModer.Core.Exceptions;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeadModer.Infrastructure.Services;

/// <summary>
/// Реализация сервиса управления физическими файлами модов Source 2 (.vpk).
/// Отвечает за установку, удаление и переключение активности модов (mod.vpk <-> mod.vpk.disabled).
/// </summary>
public sealed class FileSystemService : IFileSystemService
{
    private const string DisabledExtension = ".disabled";
    private readonly ISteamLocator _steamLocator;
    private readonly ILocalModRepository _modRepository;
    private readonly IGameInfoService? _gameInfoService;
    private readonly ILogger<FileSystemService>? _logger;

    public FileSystemService(
        ISteamLocator steamLocator,
        ILocalModRepository modRepository,
        IGameInfoService? gameInfoService = null,
        ILogger<FileSystemService>? logger = null)
    {
        _steamLocator = steamLocator ?? throw new ArgumentNullException(nameof(steamLocator));
        _modRepository = modRepository ?? throw new ArgumentNullException(nameof(modRepository));
        _gameInfoService = gameInfoService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<InstalledMod> InstallModAsync(string tempFilePath, ModMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tempFilePath);
        ArgumentNullException.ThrowIfNull(metadata);

        if (!File.Exists(tempFilePath))
        {
            throw new FileNotFoundException($"Файл мода не найден по пути: {tempFilePath}", tempFilePath);
        }

        // Проверяем, не запущена ли сейчас игра
        EnsureGameNotRunning();

        // Автоматически гарантируем настройку gameinfo.gi для Source 2 (deadlocker.net/install-guide)
        if (_gameInfoService != null)
        {
            try
            {
                await _gameInfoService.EnableModdingAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Не удалось автоматически пропатчить gameinfo.gi при установке мода");
            }
        }

        // Сначала нормализуем существующие файлы в addons по стандарту Source 2
        await MigrateNonPakFilesAsync(cancellationToken);

        var addonsPath = await _steamLocator.GetAddonsPathAsync(cancellationToken);
        var installedVpkFiles = new List<string>();
        long totalSize = 0;
        var modId = Guid.NewGuid().ToString("N");

        try
        {
            var ext = Path.GetExtension(tempFilePath).ToLowerInvariant();

            if (ext == ".vpk" || IsVpkFile(tempFilePath))
            {
                // Одиночный .vpk файл: строго pakXX_dir.vpk согласно Source 2
                var destFileName = GetNextAvailablePakSlot(addonsPath);
                var destFilePath = Path.Combine(addonsPath, destFileName);

                File.Copy(tempFilePath, destFilePath, overwrite: true);
                installedVpkFiles.Add(destFileName);

                totalSize = new FileInfo(destFilePath).Length;
                _logger?.LogInformation("Установлен VPK файл по стандарту Source 2: {FileName} ({Size} байт)", destFileName, totalSize);
            }
            else if (ext == ".zip")
            {
                // Архив, содержащий один или несколько .vpk файлов
                using var archive = ZipFile.OpenRead(tempFilePath);
                var vpkEntries = archive.Entries.Where(e => e.FullName.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase)).ToList();

                if (vpkEntries.Count == 0)
                {
                    throw new ModInstallationException(metadata.Name, addonsPath, "Архив не содержит файлов модификаций формата .vpk");
                }

                var reservedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in vpkEntries)
                {
                    var destFileName = GetNextAvailablePakSlot(addonsPath, reservedSlots);
                    reservedSlots.Add(destFileName);
                    var destFilePath = Path.Combine(addonsPath, destFileName);

                    entry.ExtractToFile(destFilePath, overwrite: true);
                    installedVpkFiles.Add(destFileName);
                    totalSize += new FileInfo(destFilePath).Length;

                    _logger?.LogInformation("Извлечен VPK из архива по стандарту Source 2: {FileName}", destFileName);
                }
            }
            else
            {
                throw new ModInstallationException(metadata.Name, addonsPath, $"Неподдерживаемый формат файла: '{ext}'. Ожидается .vpk или .zip архив с .vpk");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Отказано в доступе при копировании мода в {AddonsPath}", addonsPath);
            throw new ModInstallationException(metadata.Name, addonsPath, $"Отказано в доступе к директории '{addonsPath}'. Запустите приложение с правами Администратора.", ex);
        }
        catch (IOException ex)
        {
            _logger?.LogError(ex, "Ошибка ввода-вывода при установке мода '{Name}'", metadata.Name);
            throw new ModInstallationException(metadata.Name, addonsPath, $"Файл заблокирован или занят другим процессом: {ex.Message}", ex);
        }

        // Формируем и сохраняем сущность InstalledMod в базу LiteDB
        var installedMod = new InstalledMod
        {
            Id = modId,
            Name = metadata.Name,
            Description = metadata.Description,
            Version = metadata.Version,
            Author = metadata.Author,
            Category = metadata.Category,
            TargetHero = metadata.TargetHero,
            RemoteModId = metadata.RemoteModId,
            IsEnabled = true,
            InstallDate = DateTime.UtcNow,
            FileSizeBytes = totalSize,
            ModDirectoryPath = addonsPath,
            VpkFiles = installedVpkFiles,
            GameCompatibility = metadata.GameCompatibility,
            Tags = metadata.Tags
        };

        await _modRepository.AddAsync(installedMod, cancellationToken);
        _logger?.LogInformation("Мод '{Name}' успешно зарегистрирован в базе данных (ID: {Id})", installedMod.Name, installedMod.Id);

        return installedMod;
    }

    /// <inheritdoc />
    public Task<bool> ToggleModStateAsync(Guid modId, CancellationToken cancellationToken = default)
    {
        return ToggleModStateAsync(modId.ToString("N"), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ToggleModStateAsync(string modId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);

        EnsureGameNotRunning();

        var mod = await _modRepository.GetByIdAsync(modId, cancellationToken);
        if (mod == null)
        {
            throw new ModNotFoundException(modId);
        }

        var addonsPath = await _steamLocator.GetAddonsPathAsync(cancellationToken);
        var targetEnabled = !mod.IsEnabled;
        var updatedVpkFiles = new List<string>();

        try
        {
            foreach (var vpkRelative in mod.VpkFiles)
            {
                var baseFileName = vpkRelative.EndsWith(DisabledExtension, StringComparison.OrdinalIgnoreCase)
                    ? vpkRelative[..^DisabledExtension.Length]
                    : vpkRelative;

                var enabledFilePath = Path.Combine(addonsPath, baseFileName);
                var disabledFilePath = enabledFilePath + DisabledExtension;

                if (targetEnabled)
                {
                    // Включение: переименовываем mod.vpk.disabled -> mod.vpk
                    if (File.Exists(disabledFilePath))
                    {
                        if (File.Exists(enabledFilePath))
                        {
                            File.Delete(enabledFilePath);
                        }
                        File.Move(disabledFilePath, enabledFilePath);
                        _logger?.LogDebug("Файл мода включен: {Old} -> {New}", disabledFilePath, enabledFilePath);
                    }
                    updatedVpkFiles.Add(baseFileName);
                }
                else
                {
                    // Отключение: переименовываем mod.vpk -> mod.vpk.disabled
                    if (File.Exists(enabledFilePath))
                    {
                        if (File.Exists(disabledFilePath))
                        {
                            File.Delete(disabledFilePath);
                        }
                        File.Move(enabledFilePath, disabledFilePath);
                        _logger?.LogDebug("Файл мода отключен: {Old} -> {New}", enabledFilePath, disabledFilePath);
                    }
                    updatedVpkFiles.Add(baseFileName + DisabledExtension);
                }
            }

            mod.IsEnabled = targetEnabled;
            mod.VpkFiles = updatedVpkFiles;
            mod.LastUpdatedDate = DateTime.UtcNow;

            await _modRepository.UpdateAsync(mod, cancellationToken);
            _logger?.LogInformation("Статус мода '{Name}' переключен на: {Status}", mod.Name, targetEnabled ? "Включен" : "Отключен");

            return targetEnabled;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Отказано в доступе при изменении состояния файлов мода {ModId}", modId);
            throw new UnauthorizedAccessException("Недостаточно прав для переименования файлов в папке addons. Запустите приложение от имени Администратора.", ex);
        }
        catch (IOException ex)
        {
            _logger?.LogError(ex, "Ошибка ввода-вывода при переименовании файлов мода {ModId}", modId);
            throw new IOException($"Файл мода заблокирован. Убедитесь, что Deadlock закрыт и файл не открыт в другой программе: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Task<bool> UninstallModAsync(Guid modId, CancellationToken cancellationToken = default)
    {
        return UninstallModAsync(modId.ToString("N"), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UninstallModAsync(string modId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);

        EnsureGameNotRunning();

        var mod = await _modRepository.GetByIdAsync(modId, cancellationToken);
        if (mod == null)
        {
            throw new ModNotFoundException(modId);
        }

        var addonsPath = await _steamLocator.GetAddonsPathAsync(cancellationToken);

        try
        {
            // Удаляем физические файлы (.vpk и .vpk.disabled)
            foreach (var file in mod.VpkFiles)
            {
                var fullPath = Path.Combine(addonsPath, file);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger?.LogInformation("Удален физический файл мода: {Path}", fullPath);
                }

                // Проверяем альтернативные расширения на случай рассинхронизации
                var disabledVariant = fullPath + DisabledExtension;
                if (File.Exists(disabledVariant))
                {
                    File.Delete(disabledVariant);
                }

                if (fullPath.EndsWith(DisabledExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var enabledVariant = fullPath[..^DisabledExtension.Length];
                    if (File.Exists(enabledVariant))
                    {
                        File.Delete(enabledVariant);
                    }
                }
            }

            // Удаляем локальное превью, если есть
            if (!string.IsNullOrWhiteSpace(mod.LocalPreviewPath) && File.Exists(mod.LocalPreviewPath))
            {
                try
                {
                    File.Delete(mod.LocalPreviewPath);
                }
                catch
                {
                    // Игнорируем ошибки удаления превью
                }
            }

            // Удаляем запись из LiteDB
            var deletedFromDb = await _modRepository.DeleteAsync(mod.Id, cancellationToken);
            _logger?.LogInformation("Мод '{Name}' ({Id}) полностью удален из системы", mod.Name, mod.Id);

            return deletedFromDb;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Отказано в доступе при удалении файлов мода {ModId}", modId);
            throw new UnauthorizedAccessException("Недостаточно прав для удаления файлов из папки addons Deadlock.", ex);
        }
    }

    /// <inheritdoc />
    public InstalledMod InstallMod(string tempFilePath, ModMetadata metadata)
    {
        return InstallModAsync(tempFilePath, metadata).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public bool ToggleModState(Guid modId)
    {
        return ToggleModStateAsync(modId).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public bool UninstallMod(Guid modId)
    {
        return UninstallModAsync(modId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Проверка сигнатуры файла Valve Source 2 VPK (0x55AA1234).
    /// </summary>
    private static bool IsVpkFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 4) return false;
            var header = new byte[4];
            var read = fs.Read(header, 0, 4);
            return read == 4 && header[0] == 0x34 && header[1] == 0x12 && header[2] == 0xAA && header[3] == 0x55;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Проверка, не запущен ли процесс Deadlock в данный момент (Source 2 блокирует .vpk файлы при работе).
    /// </summary>
    private void EnsureGameNotRunning()
    {
        var runningProcesses = Process.GetProcessesByName("project8");
        if (runningProcesses.Length == 0)
        {
            runningProcesses = Process.GetProcessesByName("citadel");
        }

        if (runningProcesses.Length > 0)
        {
            _logger?.LogWarning("Обнаружен запущенный процесс игры Deadlock. Модификация файлов заблокирована.");
            throw new InvalidOperationException("Игра Deadlock в данный момент запущена. Пожалуйста, закройте игру перед установкой, переключением или удалением модов.");
        }
    }

    /// <inheritdoc />
    public async Task MigrateNonPakFilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var addonsPath = await _steamLocator.GetAddonsPathAsync(cancellationToken);
            if (!Directory.Exists(addonsPath))
                return;

            var dirFiles = Directory.GetFiles(addonsPath, "*.*")
                .Where(f => f.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".vpk.disabled", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var nonPakFiles = dirFiles.Where(f => !PakFileRegex.IsMatch(Path.GetFileName(f))).ToList();
            if (nonPakFiles.Count == 0)
                return;

            _logger?.LogInformation("Обнаружено {Count} файлов модов, требующих нормализации по стандарту Source 2 (pakXX_dir.vpk)", nonPakFiles.Count);

            var allDbMods = (await _modRepository.GetAllAsync(cancellationToken)).ToList();

            foreach (var filePath in nonPakFiles)
            {
                var oldFileName = Path.GetFileName(filePath);
                var isDisabled = oldFileName.EndsWith(DisabledExtension, StringComparison.OrdinalIgnoreCase);

                var newPakSlot = GetNextAvailablePakSlot(addonsPath);
                var newFileName = isDisabled ? newPakSlot + DisabledExtension : newPakSlot;
                var targetFilePath = Path.Combine(addonsPath, newFileName);

                try
                {
                    File.Move(filePath, targetFilePath);
                    _logger?.LogInformation("Файл мода нормализован: '{Old}' -> '{New}'", oldFileName, newFileName);

                    // Обновляем запись в LiteDB, если этот мод там уже зарегистрирован
                    var matchingMod = allDbMods.FirstOrDefault(m => m.VpkFiles.Contains(oldFileName, StringComparer.OrdinalIgnoreCase));
                    if (matchingMod != null)
                    {
                        var updatedList = matchingMod.VpkFiles
                            .Select(f => f.Equals(oldFileName, StringComparison.OrdinalIgnoreCase) ? newFileName : f)
                            .ToList();
                        matchingMod.VpkFiles = updatedList;
                        matchingMod.LastUpdatedDate = DateTime.UtcNow;
                        await _modRepository.UpdateAsync(matchingMod, cancellationToken);
                        _logger?.LogInformation("Запись в БД для мода '{ModName}' обновлена с новым именем {NewFileName}", matchingMod.Name, newFileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка при миграции файла мода '{FileName}' в формат pakXX_dir.vpk", oldFileName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при автоматической нормализации файлов папки addons");
        }
    }

    /// <summary>
    /// Регулярное выражение для проверки имени файла мода согласно стандарту Source 2 / Deadlock (pak01_dir.vpk ... pak99_dir.vpk).
    /// </summary>
    private static readonly Regex PakFileRegex = new(@"^pak(?<num>\d{2,3})_dir\.vpk(\.disabled)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Находит первый доступный номер слота pakXX_dir.vpk (от 01 до 99) в папке addons.
    /// </summary>
    public static string GetNextAvailablePakSlot(string addonsPath, HashSet<string>? reserved = null)
    {
        var usedSlots = new HashSet<int>();

        if (Directory.Exists(addonsPath))
        {
            var existingFiles = Directory.GetFiles(addonsPath, "*.*");
            foreach (var file in existingFiles)
            {
                var fileName = Path.GetFileName(file);
                var match = PakFileRegex.Match(fileName);
                if (match.Success && int.TryParse(match.Groups["num"].Value, out var num))
                {
                    usedSlots.Add(num);
                }
            }
        }

        if (reserved != null)
        {
            foreach (var reservedName in reserved)
            {
                var match = PakFileRegex.Match(reservedName);
                if (match.Success && int.TryParse(match.Groups["num"].Value, out var num))
                {
                    usedSlots.Add(num);
                }
            }
        }

        // Поиск первого свободного слота от 02 до 99 (слот 01 зарезервирован базовой игрой: citadel/pak01_dir.vpk)
        for (int i = 2; i <= 99; i++)
        {
            if (!usedSlots.Contains(i))
            {
                return $"pak{i:D2}_dir.vpk";
            }
        }

        // Запасной вариант для очень большого числа модов (от 100 до 999)
        for (int i = 100; i <= 999; i++)
        {
            if (!usedSlots.Contains(i))
            {
                return $"pak{i:D2}_dir.vpk";
            }
        }

        return "pak99_dir.vpk";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalids = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(fileName.Select(c => invalids.Contains(c) || c == ' ' ? '_' : c));
        return string.IsNullOrWhiteSpace(sanitized) ? "mod" : sanitized;
    }
}
