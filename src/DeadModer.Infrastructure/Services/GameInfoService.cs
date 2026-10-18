using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DeadModer.Infrastructure.Services;

/// <summary>
/// Реализация сервиса управления gameinfo.gi Source 2 в строгом соответствии с Deadlocker.net install-guide.
/// Обеспечивает монтирование папки citadel/addons и AddonRoot в SearchPaths.
/// </summary>
public sealed class GameInfoService : IGameInfoService
{
    private const string BackupExtension = ".bak";
    private readonly ISteamLocator _steamLocator;
    private readonly ILogger<GameInfoService>? _logger;

    public GameInfoService(ISteamLocator steamLocator, ILogger<GameInfoService>? logger = null)
    {
        _steamLocator = steamLocator ?? throw new ArgumentNullException(nameof(steamLocator));
        _logger = logger;
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<string?> GetGameInfoPathAsync(CancellationToken cancellationToken = default)
    {
        // 1. Прямая проверка известного стандартного пути
        const string exactKnownPath = @"D:\Program Files\Steam\steamapps\common\Deadlock\game\citadel\gameinfo.gi";
        if (File.Exists(exactKnownPath))
        {
            return exactKnownPath;
        }

        // 2. Поиск через SteamLocator
        var gamePath = await _steamLocator.FindDeadlockGamePathAsync(cancellationToken);
        if (!string.IsNullOrEmpty(gamePath))
        {
            var path = Path.Combine(gamePath, "game", "citadel", "gameinfo.gi");
            if (File.Exists(path)) return path;
        }

        // 3. Сканирование всех доступных дисков системы
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var candidate1 = Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam", "steamapps", "common", "Deadlock", "game", "citadel", "gameinfo.gi");
                if (File.Exists(candidate1)) return candidate1;

                var candidate2 = Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam", "steamapps", "common", "Deadlock", "game", "citadel", "gameinfo.gi");
                if (File.Exists(candidate2)) return candidate2;

                var candidate3 = Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "common", "Deadlock", "game", "citadel", "gameinfo.gi");
                if (File.Exists(candidate3)) return candidate3;

                var candidate4 = Path.Combine(drive.RootDirectory.FullName, "Steam", "steamapps", "common", "Deadlock", "game", "citadel", "gameinfo.gi");
                if (File.Exists(candidate4)) return candidate4;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Ошибка при поиске gameinfo.gi по дискам");
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> IsModdingEnabledAsync(CancellationToken cancellationToken = default)
    {
        var gameInfoPath = await GetGameInfoPathAsync(cancellationToken);
        if (string.IsNullOrEmpty(gameInfoPath) || !File.Exists(gameInfoPath))
        {
            return false;
        }

        try
        {
            var content = await File.ReadAllTextAsync(gameInfoPath, cancellationToken);
            // Проверяем наличие строки Game citadel/addons в начале блока SearchPaths
            return Regex.IsMatch(content, @"SearchPaths\s*\{[\s\S]*?Game\s+citadel/addons", RegexOptions.IgnoreCase);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при проверке статуса gameinfo.gi");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> EnableModdingAsync(CancellationToken cancellationToken = default)
    {
        var gameInfoPath = await GetGameInfoPathAsync(cancellationToken);
        if (string.IsNullOrEmpty(gameInfoPath) || !File.Exists(gameInfoPath))
        {
            _logger?.LogWarning("gameinfo.gi не найден для настройки поддержки модов");
            return false;
        }

        try
        {
            var content = await File.ReadAllTextAsync(gameInfoPath, cancellationToken);

            // Точный блок SearchPaths, требуемый Source 2 / Deadlock
            const string targetSearchPathsBlock = 
@"		SearchPaths
		{
			Game				citadel/addons
			Mod					citadel
			Write				citadel
			Game				citadel
			Write				core
			Mod					core
			Game				core
		}";

            // Проверяем, настроен ли уже точно этот блок с Game citadel/addons
            if (Regex.IsMatch(content, @"SearchPaths\s*\{\s*Game\s+citadel/addons\s+Mod\s+citadel\s+Write\s+citadel\s+Game\s+citadel\s+Write\s+core\s+Mod\s+core\s+Game\s+core\s*\}", RegexOptions.IgnoreCase))
            {
                _logger?.LogInformation("gameinfo.gi уже идеально настроен с блоком SearchPaths");
                return true;
            }

            // Создаем резервную копию gameinfo.gi.bak, если ее еще нет
            var backupPath = gameInfoPath + BackupExtension;
            if (!File.Exists(backupPath))
            {
                File.Copy(gameInfoPath, backupPath, overwrite: false);
                _logger?.LogInformation("Создана резервная копия gameinfo.gi -> {BackupPath}", backupPath);
            }

            string updatedContent;
            var searchPathsRegex = new Regex(@"SearchPaths\s*\{[\s\S]*?\}", RegexOptions.IgnoreCase);

            if (searchPathsRegex.IsMatch(content))
            {
                // Заменяем весь существующий блок SearchPaths { ... } на требуемый
                updatedContent = searchPathsRegex.Replace(content, targetSearchPathsBlock, 1);
                _logger?.LogInformation("Блок SearchPaths в gameinfo.gi успешно заменен на стандарт Deadlock Source 2");
            }
            else
            {
                _logger?.LogError("Секция SearchPaths не найдена в gameinfo.gi");
                return false;
            }

            await File.WriteAllTextAsync(gameInfoPath, updatedContent, cancellationToken);
            _logger?.LogInformation("gameinfo.gi успешно сохранен по пути: {Path}", gameInfoPath);

            // Убеждаемся, что папка addons создана
            var addonsDir = Path.Combine(Path.GetDirectoryName(gameInfoPath)!, "addons");
            if (!Directory.Exists(addonsDir))
            {
                Directory.CreateDirectory(addonsDir);
                _logger?.LogInformation("Папка addons создана: {AddonsDir}", addonsDir);
            }

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger?.LogError(ex, "Отказано в доступе при изменении gameinfo.gi. Требуются права администратора.");
            throw new UnauthorizedAccessException("Недостаточно прав для редактирования gameinfo.gi. Запустите приложение от имени Администратора.", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при включении поддержки модов в gameinfo.gi");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestoreOriginalGameInfoAsync(CancellationToken cancellationToken = default)
    {
        var gameInfoPath = await GetGameInfoPathAsync(cancellationToken);
        if (string.IsNullOrEmpty(gameInfoPath)) return false;

        var backupPath = gameInfoPath + BackupExtension;
        if (!File.Exists(backupPath))
        {
            _logger?.LogWarning("Резервная копия {BackupPath} не найдена для восстановления", backupPath);
            return false;
        }

        try
        {
            File.Copy(backupPath, gameInfoPath, overwrite: true);
            _logger?.LogInformation("Исходный gameinfo.gi успешно восстановлен из резервной копии");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при восстановлении исходного gameinfo.gi");
            return false;
        }
    }
}
