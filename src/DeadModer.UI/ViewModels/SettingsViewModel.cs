using System;
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
/// ViewModel для вкладки "Настройки" (минималистично, два языка, выбор фонов).
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly ISteamLocator _steamLocator;
    private readonly ILogger<SettingsViewModel>? _logger;

    [ObservableProperty]
    private string _gamePath = string.Empty;

    [ObservableProperty]
    private string _modsStoragePath = string.Empty;

    [ObservableProperty]
    private string _backupsPath = string.Empty;

    [ObservableProperty]
    private bool _autoCheckUpdates = true;

    [ObservableProperty]
    private bool _launchGameWithMods = true;

    [ObservableProperty]
    private bool _closeToTray = false;

    [ObservableProperty]
    private string _selectedLanguage = "ru";

    [ObservableProperty]
    private string _selectedBackground = "Fone_1.png";

    [ObservableProperty]
    private string _detectionStatus = string.Empty;

    [ObservableProperty]
    private bool _isSaving = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool IsRussianSelected => SelectedLanguage == "ru";
    public bool IsEnglishSelected => SelectedLanguage == "en";

    public SettingsViewModel()
    {
        _settingsRepository = null!;
        _steamLocator = null!;
        _logger = null;
        GamePath = @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock";
        DetectionStatus = Loc["Settings_PathVerified"];
    }

    public SettingsViewModel(
        IAppSettingsRepository settingsRepository,
        ISteamLocator steamLocator,
        ILogger<SettingsViewModel>? logger = null)
    {
        _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        _steamLocator = steamLocator ?? throw new ArgumentNullException(nameof(steamLocator));
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsRepository.GetSettingsAsync(cancellationToken);
            GamePath = settings.GamePath;
            ModsStoragePath = settings.ModsStoragePath;
            BackupsPath = settings.BackupsPath;
            AutoCheckUpdates = settings.AutoCheckUpdates;
            LaunchGameWithMods = settings.LaunchGameWithMods;
            CloseToTray = settings.CloseToTray;
            SelectedLanguage = settings.Language ?? "ru";
            SelectedBackground = settings.SelectedBackground ?? "Fone_1.png";

            Loc.SetLanguage(SelectedLanguage);
            NotifySelections();

            if (Directory.Exists(GamePath))
            {
                DetectionStatus = Loc["Settings_PathVerified"];
            }
            else
            {
                DetectionStatus = Loc["Settings_PathNotFound"];
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка загрузки настроек");
        }
    }

    [RelayCommand]
    public void SelectLanguage(string language)
    {
        SelectedLanguage = language;
        Loc.SetLanguage(language);
        NotifySelections();
    }

    private void NotifySelections()
    {
        OnPropertyChanged(nameof(IsRussianSelected));
        OnPropertyChanged(nameof(IsEnglishSelected));
    }

    [RelayCommand]
    public async Task AutoDetectSteamPathAsync(CancellationToken cancellationToken = default)
    {
        DetectionStatus = Loc.IsEnglish ? "Searching in Steam libraries..." : "Поиск игры в библиотеках Steam...";
        try
        {
            var detected = await _steamLocator.FindDeadlockGamePathAsync(cancellationToken);
            if (!string.IsNullOrEmpty(detected))
            {
                GamePath = detected;
                DetectionStatus = Loc["Settings_DetectedSuccess"];
                await SaveSettingsAsync(cancellationToken);
            }
            else
            {
                DetectionStatus = Loc["Settings_PathNotFound"];
            }
        }
        catch (Exception ex)
        {
            DetectionStatus = $"{ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SaveSettingsAsync(CancellationToken cancellationToken = default)
    {
        IsSaving = true;
        StatusMessage = Loc.IsEnglish ? "Saving..." : "Сохранение...";

        try
        {
            var settings = await _settingsRepository.GetSettingsAsync(cancellationToken);
            settings.GamePath = GamePath.Trim();
            settings.ModsStoragePath = ModsStoragePath.Trim();
            settings.BackupsPath = BackupsPath.Trim();
            settings.AutoCheckUpdates = AutoCheckUpdates;
            settings.LaunchGameWithMods = LaunchGameWithMods;
            settings.CloseToTray = CloseToTray;
            settings.Language = SelectedLanguage;
            settings.SelectedBackground = SelectedBackground;

            await _settingsRepository.SaveSettingsAsync(settings, cancellationToken);
            StatusMessage = Loc["Settings_SavedSuccess"];
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка сохранения настроек");
            StatusMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
