using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeadModer.Core.Entities;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.UI.Services;
using Microsoft.Extensions.Logging;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// Перечисление разделов бокового меню приложения.
/// </summary>
public enum NavigationTab
{
    Home = 0,      // Главная
    Mods = 1,      // Каталог модов
    Heroes = 2,    // Герои
    MyMods = 3,    // Мои моды (Установленные)
    Settings = 4   // Настройки
}

/// <summary>
/// Главная ViewModel приложения DeadModer.
/// Управляет навигацией, фоновыми темами, языком, инспектором и процессами игры.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISteamLocator _steamLocator;
    private readonly IFileSystemService _fileSystemService;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IGameInfoService? _gameInfoService;
    private readonly ILogger<MainWindowViewModel>? _logger;
    private Timer? _gameStatusTimer;

    [ObservableProperty]
    private NavigationTab _currentTab = NavigationTab.Home;

    public bool IsHomeTab => CurrentTab == NavigationTab.Home;
    public bool IsModsTab => CurrentTab == NavigationTab.Mods;
    public bool IsHeroesTab => CurrentTab == NavigationTab.Heroes;
    public bool IsMyModsTab => CurrentTab == NavigationTab.MyMods;
    public bool IsSettingsTab => CurrentTab == NavigationTab.Settings;

    [ObservableProperty]
    private ViewModelBase _currentPageViewModel;

    [ObservableProperty]
    private string _gameStatusText = "Ready to launch";

    [ObservableProperty]
    private bool _isGameRunning = false;

    [ObservableProperty]
    private AppSettings? _settings;

    [ObservableProperty]
    private ModCardViewModel? _inspectorMod;

    [ObservableProperty]
    private string _currentBackgroundImage = "avares://DeadModer.UI/Assets/Backgrounds/Fone_1.png";

    [ObservableProperty]
    private bool _isAccountsModalOpen = false;

    [ObservableProperty]
    private int _accountMode = 0; // 0 = GameBanana, 1 = Deadlocker.net

    [ObservableProperty]
    private string _gameBananaUsername = string.Empty;

    [ObservableProperty]
    private string _gameBananaEmail = string.Empty;

    [ObservableProperty]
    private string _gameBananaPassword = string.Empty;

    [ObservableProperty]
    private string _deadlockerUsername = string.Empty;

    [ObservableProperty]
    private string _deadlockerEmail = string.Empty;

    [ObservableProperty]
    private string _deadlockerPassword = string.Empty;

    [ObservableProperty]
    private bool _isGameBananaAuthenticated = false;

    [ObservableProperty]
    private bool _isDeadlockerAuthenticated = false;

    [ObservableProperty]
    private bool _unlockRestrictedMods = true;

    [ObservableProperty]
    private string _accountsStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _accountsStatusIsSuccess = true;

    public bool IsModeGameBanana => AccountMode == 0;
    public bool IsModeDeadlocker => AccountMode == 1;

    public bool IsAuthenticated => IsGameBananaAuthenticated || IsDeadlockerAuthenticated;

    partial void OnAccountModeChanged(int value)
    {
        OnPropertyChanged(nameof(IsModeGameBanana));
        OnPropertyChanged(nameof(IsModeDeadlocker));
    }

    public string UserDisplayName
    {
        get
        {
            if (IsGameBananaAuthenticated && IsDeadlockerAuthenticated)
                return $"{GameBananaUsername} | {DeadlockerUsername}";
            if (IsGameBananaAuthenticated && !string.IsNullOrWhiteSpace(GameBananaUsername))
                return GameBananaUsername;
            if (IsDeadlockerAuthenticated && !string.IsNullOrWhiteSpace(DeadlockerUsername))
                return DeadlockerUsername;
            return Loc.IsEnglish ? "Sign In / Register" : "Вход / Регистрация";
        }
    }

    public bool HasLinkedAccount => IsAuthenticated;

    public HomeViewModel Home { get; }
    public ModCatalogViewModel Catalog { get; }
    public InstalledModsViewModel InstalledMods { get; }
    public HeroesViewModel Heroes { get; }
    public SettingsViewModel SettingsViewModel { get; }

    /// <summary>
    /// Конструктор для предварительного просмотра в дизайнере Avalonia (Design-time).
    /// </summary>
    public MainWindowViewModel()
    {
        _steamLocator = null!;
        _fileSystemService = null!;
        _settingsRepository = null!;
        _gameInfoService = null;
        _logger = null;

        Home = new HomeViewModel();
        Catalog = new ModCatalogViewModel();
        InstalledMods = new InstalledModsViewModel();
        Heroes = new HeroesViewModel();
        SettingsViewModel = new SettingsViewModel();

        _currentPageViewModel = Home;
        _inspectorMod = Catalog.SelectedModCard;
        UpdateGameStatusText();
        Loc.LanguageChanged += UpdateGameStatusText;
    }

    /// <summary>
    /// Основной конструктор с внедрением всех зависимостей через DI.
    /// </summary>
    public MainWindowViewModel(
        HomeViewModel homeViewModel,
        ModCatalogViewModel catalogViewModel,
        InstalledModsViewModel installedModsViewModel,
        HeroesViewModel heroesViewModel,
        SettingsViewModel settingsViewModel,
        ISteamLocator steamLocator,
        IFileSystemService fileSystemService,
        IAppSettingsRepository settingsRepository,
        IGameInfoService? gameInfoService = null,
        ILogger<MainWindowViewModel>? logger = null)
    {
        Home = homeViewModel ?? throw new ArgumentNullException(nameof(homeViewModel));
        Catalog = catalogViewModel ?? throw new ArgumentNullException(nameof(catalogViewModel));
        InstalledMods = installedModsViewModel ?? throw new ArgumentNullException(nameof(installedModsViewModel));
        Heroes = heroesViewModel ?? throw new ArgumentNullException(nameof(heroesViewModel));
        SettingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
        _steamLocator = steamLocator ?? throw new ArgumentNullException(nameof(steamLocator));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _gameInfoService = gameInfoService;
        _logger = logger;

        _currentPageViewModel = Home;

        // Синхронизация выбранной карточки из каталога в инспектор
        Catalog.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Catalog.SelectedModCard) && Catalog.SelectedModCard != null)
            {
                InspectorMod = Catalog.SelectedModCard;
            }
        };

        // Синхронизация выбранной карточки из списка установленных в инспектор
        InstalledMods.SetSelectionCallback(card =>
        {
            InspectorMod = card;
        });

        Home.OnNavigate = tab => Navigate(tab);
        Home.OnFilterHero = heroName =>
        {
            Navigate(NavigationTab.Mods);
            Catalog.SearchQuery = heroName;
        };

        // Навигация из вкладки героев в каталог с поиском по имени героя и синхронизация списка героев
        heroesViewModel.SetFilterCallback(heroName =>
        {
            Navigate(NavigationTab.Mods);
            Catalog.SearchQuery = heroName;
        });
        heroesViewModel.HeroesLoaded += heroNames =>
        {
            Catalog.UpdateAvailableHeroes(heroNames);
        };
        Heroes = heroesViewModel;

        // Привязка действий управления аккаунтами из каталога
        Catalog.OnRequestOpenAccounts = () => OpenAccounts();

        Loc.LanguageChanged += UpdateGameStatusText;
    }

    /// <summary>
    /// Первоначальная асинхронная инициализация при открытии окна.
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            _logger?.LogInformation("Инициализация главного окна DeadModer...");

            // Автоматический поиск и настройка gameinfo.gi для Deadlock Source 2
            if (_gameInfoService != null)
            {
                try
                {
                    _logger?.LogInformation("Автоматическая проверка и настройка gameinfo.gi для Deadlock...");
                    await _gameInfoService.EnableModdingAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Не удалось автоматически настроить gameinfo.gi при запуске");
                }
            }

            Settings = await _settingsRepository.GetSettingsAsync();

            GameBananaUsername = Settings.GameBananaUsername ?? string.Empty;
            GameBananaEmail = Settings.GameBananaEmail ?? string.Empty;
            DeadlockerUsername = Settings.DeadlockerUsername ?? string.Empty;
            DeadlockerEmail = Settings.DeadlockerEmail ?? string.Empty;
            IsGameBananaAuthenticated = Settings.IsGameBananaAuthenticated || !string.IsNullOrWhiteSpace(GameBananaUsername);
            IsDeadlockerAuthenticated = Settings.IsDeadlockerAuthenticated || !string.IsNullOrWhiteSpace(DeadlockerUsername);
            UnlockRestrictedMods = Settings.UnlockRestrictedMods;

            SyncAccountsWithCatalog();

            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(HasLinkedAccount));
            OnPropertyChanged(nameof(IsAuthenticated));

            if (!string.IsNullOrWhiteSpace(Settings.Language))
            {
                Loc.SetLanguage(Settings.Language);
            }

            if (!string.IsNullOrWhiteSpace(Settings.SelectedBackground))
            {
                SetBackground(Settings.SelectedBackground);
            }

            await CheckGameStatusAsync();
            await Home.LoadHomeDataAsync();
            await Catalog.LoadCatalogAsync();
            await InstalledMods.SyncAddonsFolderAsync();
            await SettingsViewModel.LoadSettingsAsync();

            InspectorMod = Catalog.SelectedModCard ?? (InstalledMods.InstalledMods.Count > 0 ? InstalledMods.InstalledMods[0] : null);

            // Мониторинг процесса игры раз в 4 секунды
            _gameStatusTimer = new Timer(async _ =>
            {
                await CheckGameStatusAsync();
            }, null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при инициализации главного окна");
        }
    }

    [RelayCommand]
    public async Task ToggleLanguageAsync()
    {
        var newLang = Loc.IsRussian ? "en" : "ru";
        Loc.SetLanguage(newLang);
        UpdateGameStatusText();

        if (Settings != null)
        {
            Settings.Language = newLang;
            await _settingsRepository.SaveSettingsAsync(Settings);
        }
        SettingsViewModel.SelectedLanguage = newLang;
    }

    [RelayCommand]
    public async Task SetLanguageAsync(string lang)
    {
        Loc.SetLanguage(lang);
        UpdateGameStatusText();

        if (Settings != null)
        {
            Settings.Language = lang;
            await _settingsRepository.SaveSettingsAsync(Settings);
        }
        SettingsViewModel.SelectedLanguage = lang;
    }

    [RelayCommand]
    public void SetBackground(string bgFileName)
    {
        CurrentBackgroundImage = $"avares://DeadModer.UI/Assets/Backgrounds/{bgFileName}";
    }

    private void UpdateGameStatusText()
    {
        GameStatusText = IsGameRunning ? Loc["Status_GameRunning"] : Loc["Status_ReadyToLaunch"];
    }

    /// <summary>
    /// Переключение активной вкладки интерфейса.
    /// </summary>
    [RelayCommand]
    public void Navigate(NavigationTab tab)
    {
        CurrentTab = tab;
        OnPropertyChanged(nameof(IsHomeTab));
        OnPropertyChanged(nameof(IsModsTab));
        OnPropertyChanged(nameof(IsHeroesTab));
        OnPropertyChanged(nameof(IsMyModsTab));
        OnPropertyChanged(nameof(IsSettingsTab));

        CurrentPageViewModel = tab switch
        {
            NavigationTab.Home => Home,
            NavigationTab.Mods => Catalog,
            NavigationTab.Heroes => Heroes,
            NavigationTab.MyMods => InstalledMods,
            NavigationTab.Settings => SettingsViewModel,
            _ => Home
        };

        if (tab == NavigationTab.MyMods)
        {
            _ = InstalledMods.LoadInstalledModsAsync();
            if (InstalledMods.SelectedMod != null)
            {
                InspectorMod = InstalledMods.SelectedMod;
            }
        }
        else if (tab == NavigationTab.Settings)
        {
            _ = SettingsViewModel.LoadSettingsAsync();
        }
        else if (tab is NavigationTab.Home or NavigationTab.Mods)
        {
            if (Catalog.SelectedModCard != null)
            {
                InspectorMod = Catalog.SelectedModCard;
            }
        }
    }

    [RelayCommand]
    public Task CheckGameStatusAsync()
    {
        return Task.Run(() =>
        {
            var processes = Process.GetProcessesByName("project8");
            if (processes.Length == 0) processes = Process.GetProcessesByName("citadel");

            var running = processes.Length > 0;
            IsGameRunning = running;
            UpdateGameStatusText();
        });
    }

    [RelayCommand]
    public void LaunchDeadlock()
    {
        try
        {
            _logger?.LogInformation("Запуск Deadlock (1422450)...");
            Process.Start(new ProcessStartInfo
            {
                FileName = "steam://rungameid/1422450",
                UseShellExecute = true
            });
            GameStatusText = Loc["Status_Launching"];
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка запуска Deadlock через Steam");
        }
    }

    [RelayCommand]
    public async Task OpenModFolderAsync()
    {
        try
        {
            var addonsPath = await _steamLocator.GetAddonsPathAsync();
            if (Directory.Exists(addonsPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = addonsPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось открыть папку аддонов");
        }
    }

    [RelayCommand]
    public async Task DeleteInspectorModAsync()
    {
        if (InspectorMod == null) return;

        try
        {
            await _fileSystemService.UninstallModAsync(InspectorMod.Id);
            InspectorMod.IsInstalled = false;
            InspectorMod.UpdateActionButtonText();

            await InstalledMods.LoadInstalledModsAsync();
            _logger?.LogInformation("Мод '{Name}' успешно удален", InspectorMod.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка удаления мода {ModId}", InspectorMod.Id);
        }
    }

    [RelayCommand]
    public void OpenAccounts()
    {
        IsAccountsModalOpen = true;
        AccountsStatusMessage = string.Empty;
    }

    [RelayCommand]
    public void CloseAccounts()
    {
        IsAccountsModalOpen = false;
        AccountsStatusMessage = string.Empty;
    }

    [RelayCommand]
    public void SetAccountMode(object? modeObj)
    {
        if (modeObj is int modeInt)
        {
            AccountMode = modeInt;
        }
        else if (modeObj is string modeStr && int.TryParse(modeStr, out var parsed))
        {
            AccountMode = parsed;
        }
        AccountsStatusMessage = string.Empty;
    }

    /// <summary>
    /// Подключение аккаунта GameBanana.
    /// </summary>
    [RelayCommand]
    public async Task ConnectGameBananaAsync()
    {
        try
        {
            var username = GameBananaUsername?.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                AccountsStatusIsSuccess = false;
                AccountsStatusMessage = Loc.IsEnglish
                    ? "Please enter your GameBanana username"
                    : "Введите ваш никнейм на GameBanana";
                return;
            }

            IsGameBananaAuthenticated = true;
            UnlockRestrictedMods = true;

            Settings ??= await _settingsRepository.GetSettingsAsync();
            Settings.GameBananaUsername = username;
            Settings.GameBananaEmail = GameBananaEmail?.Trim() ?? string.Empty;
            Settings.GameBananaAuthToken = !string.IsNullOrWhiteSpace(GameBananaPassword)
                ? GameBananaPassword
                : $"gb_{Guid.NewGuid():N}";
            Settings.IsGameBananaAuthenticated = true;
            Settings.UnlockRestrictedMods = true;

            await _settingsRepository.SaveSettingsAsync(Settings);

            SyncAccountsWithCatalog();

            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(HasLinkedAccount));
            OnPropertyChanged(nameof(IsAuthenticated));

            AccountsStatusIsSuccess = true;
            AccountsStatusMessage = Loc.IsEnglish
                ? $"GameBanana account '{username}' connected!"
                : $"Аккаунт GameBanana '{username}' успешно подключен!";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при подключении GameBanana");
            AccountsStatusIsSuccess = false;
            AccountsStatusMessage = Loc.IsEnglish ? "Connection error" : "Ошибка подключения";
        }
    }

    /// <summary>
    /// Отключение аккаунта GameBanana.
    /// </summary>
    [RelayCommand]
    public async Task DisconnectGameBananaAsync()
    {
        try
        {
            IsGameBananaAuthenticated = false;
            GameBananaUsername = string.Empty;
            GameBananaEmail = string.Empty;
            GameBananaPassword = string.Empty;

            Settings ??= await _settingsRepository.GetSettingsAsync();
            Settings.GameBananaUsername = string.Empty;
            Settings.GameBananaEmail = string.Empty;
            Settings.GameBananaAuthToken = string.Empty;
            Settings.IsGameBananaAuthenticated = false;

            await _settingsRepository.SaveSettingsAsync(Settings);

            SyncAccountsWithCatalog();

            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(HasLinkedAccount));
            OnPropertyChanged(nameof(IsAuthenticated));

            AccountsStatusIsSuccess = true;
            AccountsStatusMessage = Loc.IsEnglish
                ? "GameBanana account disconnected"
                : "Аккаунт GameBanana отключен";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при отключении GameBanana");
        }
    }

    /// <summary>
    /// Подключение аккаунта Deadlocker.net.
    /// </summary>
    [RelayCommand]
    public async Task ConnectDeadlockerAsync()
    {
        try
        {
            var username = DeadlockerUsername?.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                AccountsStatusIsSuccess = false;
                AccountsStatusMessage = Loc.IsEnglish
                    ? "Please enter your Deadlocker username"
                    : "Введите ваш никнейм на Deadlocker";
                return;
            }

            IsDeadlockerAuthenticated = true;
            UnlockRestrictedMods = true;

            Settings ??= await _settingsRepository.GetSettingsAsync();
            Settings.DeadlockerUsername = username;
            Settings.DeadlockerEmail = DeadlockerEmail?.Trim() ?? string.Empty;
            Settings.DeadlockerAuthToken = !string.IsNullOrWhiteSpace(DeadlockerPassword)
                ? DeadlockerPassword
                : $"dl_{Guid.NewGuid():N}";
            Settings.IsDeadlockerAuthenticated = true;
            Settings.UnlockRestrictedMods = true;

            await _settingsRepository.SaveSettingsAsync(Settings);

            SyncAccountsWithCatalog();

            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(HasLinkedAccount));
            OnPropertyChanged(nameof(IsAuthenticated));

            AccountsStatusIsSuccess = true;
            AccountsStatusMessage = Loc.IsEnglish
                ? $"Deadlocker account '{username}' connected!"
                : $"Аккаунт Deadlocker '{username}' успешно подключен!";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при подключении Deadlocker");
            AccountsStatusIsSuccess = false;
            AccountsStatusMessage = Loc.IsEnglish ? "Connection error" : "Ошибка подключения";
        }
    }

    /// <summary>
    /// Отключение аккаунта Deadlocker.net.
    /// </summary>
    [RelayCommand]
    public async Task DisconnectDeadlockerAsync()
    {
        try
        {
            IsDeadlockerAuthenticated = false;
            DeadlockerUsername = string.Empty;
            DeadlockerEmail = string.Empty;
            DeadlockerPassword = string.Empty;

            Settings ??= await _settingsRepository.GetSettingsAsync();
            Settings.DeadlockerUsername = string.Empty;
            Settings.DeadlockerEmail = string.Empty;
            Settings.DeadlockerAuthToken = string.Empty;
            Settings.IsDeadlockerAuthenticated = false;

            await _settingsRepository.SaveSettingsAsync(Settings);

            SyncAccountsWithCatalog();

            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(HasLinkedAccount));
            OnPropertyChanged(nameof(IsAuthenticated));

            AccountsStatusIsSuccess = true;
            AccountsStatusMessage = Loc.IsEnglish
                ? "Deadlocker account disconnected"
                : "Аккаунт Deadlocker отключен";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при отключении Deadlocker");
        }
    }

    /// <summary>
    /// Синхронизация статуса аккаунтов и автора комментариев с каталогом модов.
    /// </summary>
    public void SyncAccountsWithCatalog()
    {
        Catalog.ApplyAuthenticationState(IsAuthenticated, UnlockRestrictedMods);
        Catalog.SetUserAccount(
            IsGameBananaAuthenticated,
            GameBananaUsername,
            IsDeadlockerAuthenticated,
            DeadlockerUsername
        );
    }

    /// <summary>
    /// Выход из всех привязанных аккаунтов.
    /// </summary>
    [RelayCommand]
    public async Task LogoutAccountAsync()
    {
        try
        {
            IsGameBananaAuthenticated = false;
            IsDeadlockerAuthenticated = false;
            UnlockRestrictedMods = false;

            Settings ??= await _settingsRepository.GetSettingsAsync();
            Settings.IsGameBananaAuthenticated = false;
            Settings.IsDeadlockerAuthenticated = false;
            Settings.UnlockRestrictedMods = false;
            Settings.GameBananaAuthToken = string.Empty;
            Settings.DeadlockerAuthToken = string.Empty;

            await _settingsRepository.SaveSettingsAsync(Settings);

            SyncAccountsWithCatalog();

            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(HasLinkedAccount));
            OnPropertyChanged(nameof(IsAuthenticated));

            AccountsStatusIsSuccess = true;
            AccountsStatusMessage = Loc.IsEnglish
                ? "All accounts unlinked"
                : "Все аккаунты отключены";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при выходе из аккаунтов");
        }
    }

    [RelayCommand]
    public void OpenGameBananaRegister()
    {
        OpenUrl("https://gamebanana.com/members/register");
    }

    [RelayCommand]
    public void OpenGameBananaLogin()
    {
        OpenUrl("https://gamebanana.com/members/login");
    }

    [RelayCommand]
    public void OpenDeadlockerRegister()
    {
        OpenUrl("https://deadlocker.net/register");
    }

    [RelayCommand]
    public void OpenDeadlockerLogin()
    {
        OpenUrl("https://deadlocker.net/login");
    }

    [RelayCommand]
    public void OpenDeadlocker()
    {
        OpenUrl("https://deadlocker.net");
    }

    [RelayCommand]
    public void OpenDeadlockerGuide()
    {
        OpenUrl("https://deadlocker.net/install-guide");
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Не удалось открыть URL {Url}", url);
        }
    }
}

