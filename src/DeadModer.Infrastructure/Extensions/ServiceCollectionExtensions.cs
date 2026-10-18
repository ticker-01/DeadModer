using System;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.Infrastructure.Data;
using DeadModer.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DeadModer.Infrastructure.Extensions;

/// <summary>
/// Методы расширения IServiceCollection для регистрации инфраструктурных сервисов.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует контекст LiteDB и все репозитории локальных данных.
    /// </summary>
    /// <param name="services">Коллекция сервисов DI</param>
    /// <param name="databasePath">Опциональный путь к файлу БД (null = AppData/DeadModer/deadmoder.db)</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? databasePath = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Регистрация LiteDbContext как Singleton для управления общим соединением
        services.AddSingleton<ILiteDbContext>(sp => new LiteDbContext(databasePath));

        // Регистрация репозиториев данных
        services.AddSingleton<ILocalModRepository, LocalModRepository>();
        services.AddSingleton<IAppSettingsRepository, AppSettingsRepository>();
        services.AddSingleton<ICachedModRepository, CachedModRepository>();

        // Регистрация сервисов обнаружения игры и файловой системы
        services.AddSingleton<ISteamLocator, SteamLocator>();
        services.AddSingleton<IGameInfoService, GameInfoService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();

        // Регистрация сетевых клиентов и сервисов загрузки
        services.AddHttpClient();
        services.AddHttpClient<IGameBananaClient, GameBananaClient>();
        services.AddHttpClient<IDeadlockerScraper, DeadlockerScraper>();
        services.AddHttpClient<IDownloadService, DownloadService>();

        return services;
    }
}
