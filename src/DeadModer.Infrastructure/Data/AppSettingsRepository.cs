using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Entities;
using DeadModer.Core.Enums;
using DeadModer.Core.Interfaces.Data;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace DeadModer.Infrastructure.Data;

/// <summary>
/// Реализация репозитория настроек приложения в LiteDB.
/// Хранит единственную конфигурационную запись (Singleton-паттерн, Id = 1).
/// </summary>
public sealed class AppSettingsRepository : IAppSettingsRepository
{
    private const string CollectionName = "app_settings";
    private const int SettingsId = 1;
    private readonly ILiteDbContext _context;
    private readonly ILogger<AppSettingsRepository>? _logger;

    public AppSettingsRepository(ILiteDbContext context, ILogger<AppSettingsRepository>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    private ILiteCollection<AppSettings> Collection => _context.GetCollection<AppSettings>(CollectionName);

    /// <inheritdoc />
    public Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var settings = Collection.FindById(SettingsId);
            if (settings != null)
            {
                return settings;
            }

            // Создаем настройки по умолчанию при первом запуске
            _logger?.LogInformation("Настройки не найдены. Инициализация параметров по умолчанию...");
            settings = CreateDefaultSettings();
            Collection.Insert(settings);

            return settings;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            settings.Id = SettingsId;
            var success = Collection.Upsert(settings);

            _logger?.LogInformation("Настройки приложения успешно сохранены");
            return success;
        }, cancellationToken);
    }

    private static AppSettings CreateDefaultSettings()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var baseDir = Path.Combine(localAppData, "DeadModer");

        var modsStorage = Path.Combine(baseDir, "Mods");
        var backups = Path.Combine(baseDir, "Backups");

        // Пытаемся обнаружить типичный путь к Deadlock в Steam на Windows
        var defaultSteamDeadlockPath = DetectSteamDeadlockPath();

        return new AppSettings
        {
            Id = SettingsId,
            GamePath = defaultSteamDeadlockPath,
            ModsStoragePath = modsStorage,
            BackupsPath = backups,
            Theme = AppTheme.Dark,
            Language = "ru-RU",
            AutoCheckUpdates = true,
            LaunchGameWithMods = true,
            CloseToTray = false
        };
    }

    private static string DetectSteamDeadlockPath()
    {
        string[] candidatePaths =
        [
            @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock",
            @"D:\SteamLibrary\steamapps\common\Deadlock",
            @"E:\SteamLibrary\steamapps\common\Deadlock",
            @"C:\Steam\steamapps\common\Deadlock"
        ];

        foreach (var path in candidatePaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }
}
