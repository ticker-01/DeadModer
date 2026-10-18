using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DeadModer.Infrastructure.Data;
using DeadModer.Infrastructure.Extensions;
using DeadModer.UI.ViewModels;
using DeadModer.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeadModer.UI;

/// <summary>
/// Главный класс приложения Avalonia.
/// Управляет инициализацией DI-контейнера, базы данных LiteDB и жизненным циклом окон.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Глобальный доступ к сконфигурированному DI ServiceProvider.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Построение DI-контейнера со всеми сервисами Core, Infrastructure и UI
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // 2. Инициализация локальной базы данных LiteDB при старте
        InitializeDatabase(Services);

        // 3. Запуск главного окна с разрешением ViewModel через DI
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = mainWindowViewModel;
            desktop.MainWindow = mainWindow;

            // Асинхронная загрузка каталога и проверка статуса игры при открытии окна
            desktop.MainWindow.Opened += async (_, _) =>
            {
                await mainWindowViewModel.InitializeAsync();
            };

            // Корректное закрытие LiteDB и высвобождение ресурсов при выходе
            desktop.Exit += (_, _) =>
            {
                if (Services is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Конфигурация всех зависимостей приложения в Microsoft.Extensions.DependencyInjection.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Логирование
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Инфраструктурный слой (LiteDB, SteamLocator, FileSystemService, GameBananaClient, DeadlockerScraper, DownloadService)
        services.AddInfrastructure();

        // Слой представления (ViewModels и Views)
        services.AddTransient<HomeViewModel>();
        services.AddTransient<ModCatalogViewModel>();
        services.AddTransient<InstalledModsViewModel>();
        services.AddTransient<HeroesViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<MainWindowViewModel>();

        services.AddTransient<MainWindow>();
        services.AddTransient<MainView>();
        services.AddTransient<HomeView>();
        services.AddTransient<ModCardView>();
        services.AddTransient<ModCatalogView>();
        services.AddTransient<InstalledModsView>();
        services.AddTransient<HeroesView>();
        services.AddTransient<SettingsView>();
    }

    /// <summary>
    /// Явная инициализация LiteDB при запуске приложения (создание файла, маппинг и индексы).
    /// </summary>
    private static void InitializeDatabase(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetService<ILogger<App>>();
        try
        {
            logger?.LogInformation("Инициализация локальной базы данных LiteDB...");
            var dbContext = serviceProvider.GetRequiredService<ILiteDbContext>();
            dbContext.Initialize();
            logger?.LogInformation("База данных LiteDB успешно готова к работе.");
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "Не удалось инициализировать локальную базу данных LiteDB при старте");
            throw;
        }
    }
}