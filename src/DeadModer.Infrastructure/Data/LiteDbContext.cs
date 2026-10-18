using System;
using System.IO;
using DeadModer.Core.Entities;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace DeadModer.Infrastructure.Data;

/// <summary>
/// Реализация контекста локальной базы данных LiteDB.
/// Отвечает за открытие файла БД, конфигурацию маппера и построение индексов при старте.
/// </summary>
public sealed class LiteDbContext : ILiteDbContext
{
    private readonly string _databasePath;
    private readonly ILogger<LiteDbContext>? _logger;
    private ILiteDatabase? _database;
    private bool _isInitialized;
    private readonly object _lock = new();

    public LiteDbContext(string? customDbPath = null, ILogger<LiteDbContext>? logger = null)
    {
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(customDbPath))
        {
            _databasePath = customDbPath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDir = Path.Combine(appData, "DeadModer");
            _databasePath = Path.Combine(appDir, "deadmoder.db");
        }
    }

    /// <inheritdoc />
    public ILiteDatabase Database
    {
        get
        {
            if (_database == null)
            {
                lock (_lock)
                {
                    if (_database == null)
                    {
                        Initialize();
                    }
                }
            }

            return _database!;
        }
    }

    /// <inheritdoc />
    public ILiteCollection<T> GetCollection<T>(string? name = null)
    {
        return string.IsNullOrWhiteSpace(name)
            ? Database.GetCollection<T>()
            : Database.GetCollection<T>(name);
    }

    /// <inheritdoc />
    public void Initialize()
    {
        lock (_lock)
        {
            if (_isInitialized && _database != null)
                return;

            try
            {
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger?.LogInformation("Создана директория для локальной БД: {Directory}", directory);
                }

                // Настройка BsonMapper для корректной сериализации сущностей
                ConfigureBsonMapper(BsonMapper.Global);

                // Открываем базу в режиме shared для многопоточного безопасного доступа в десктопном приложении
                var connectionString = new ConnectionString
                {
                    Filename = _databasePath,
                    Connection = ConnectionType.Shared
                };

                _database = new LiteDatabase(connectionString);
                _logger?.LogInformation("LiteDB успешно инициализирована по пути: {DbPath}", _databasePath);

                // Создаем индексы для быстрых выборок
                CreateIndexes();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Критическая ошибка при инициализации LiteDB по пути: {DbPath}", _databasePath);
                throw;
            }
        }
    }

    /// <summary>
    /// Конфигурация BsonMapper для сущностей Core.
    /// </summary>
    private static void ConfigureBsonMapper(BsonMapper mapper)
    {
        mapper.Entity<InstalledMod>()
            .Id(x => x.Id);

        mapper.Entity<AppSettings>()
            .Id(x => x.Id);

        mapper.Entity<CachedModInfo>()
            .Id(x => x.Id);

        // Игнорируем вычисляемые форматированные свойства UI при записи в базу
        mapper.Entity<InstalledMod>()
            .Ignore(x => x.FormattedSize);

        mapper.Entity<CachedModInfo>()
            .Ignore(x => x.FormattedSize)
            .Ignore(x => x.FormattedDownloads);
    }

    /// <summary>
    /// Построение индексов коллекций.
    /// </summary>
    private void CreateIndexes()
    {
        if (_database == null) return;

        // Коллекция установленных модов
        var installedMods = _database.GetCollection<InstalledMod>("installed_mods");
        installedMods.EnsureIndex(x => x.Name);
        installedMods.EnsureIndex(x => x.Category);
        installedMods.EnsureIndex(x => x.IsEnabled);
        installedMods.EnsureIndex(x => x.InstallDate);

        // Коллекция закэшированных каталожных модов
        var cachedMods = _database.GetCollection<CachedModInfo>("cached_mods");
        cachedMods.EnsureIndex(x => x.Name);
        cachedMods.EnsureIndex(x => x.Category);
        cachedMods.EnsureIndex(x => x.DownloadCount);
        cachedMods.EnsureIndex(x => x.PublishedDate);

        _logger?.LogDebug("Индексы LiteDB успешно проверены/созданы.");
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_database != null)
            {
                _logger?.LogInformation("Закрытие подключения LiteDB...");
                _database.Dispose();
                _database = null;
                _isInitialized = false;
            }
        }
    }
}
