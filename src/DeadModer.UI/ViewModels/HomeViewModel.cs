using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeadModer.Core.Entities;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.UI.Services;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// ViewModel для Главного меню (Главная / Home Dashboard).
/// Предоставляет статус системы, быстрый запуск Deadlock, быстрые действия,
/// популярные моды недели и быстрый переход к модам персонажей.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly ICachedModRepository? _cachedModRepository;
    private readonly ILocalModRepository? _localModRepository;
    private readonly IDownloadService? _downloadService;
    private readonly IFileSystemService? _fileSystemService;
    private readonly IGameBananaClient? _gameBananaClient;
    private readonly ISteamLocator? _steamLocator;
    private readonly Action<NavigationTab>? _onNavigate;
    private readonly Action<string>? _onFilterHero;
    public Action<NavigationTab>? OnNavigate { get; set; }
    public Action<string>? OnFilterHero { get; set; }

    [ObservableProperty]
    private int _totalCatalogCount = 0;

    [ObservableProperty]
    private int _installedCount = 0;

    [ObservableProperty]
    private string _gameStatusText = "Deadlock готов к запуску";

    [ObservableProperty]
    private string _addonsStatusText = "game/citadel/addons: Активна";

    public ObservableCollection<ModCardViewModel> FeaturedMods { get; } = [];

    public HomeViewModel() : this(null, null, null, null, null, null, null, null) { }

    public HomeViewModel(
        ICachedModRepository? cachedModRepository,
        ILocalModRepository? localModRepository,
        IDownloadService? downloadService,
        IFileSystemService? fileSystemService,
        IGameBananaClient? gameBananaClient,
        ISteamLocator? steamLocator,
        Action<NavigationTab>? onNavigate = null,
        Action<string>? onFilterHero = null)
    {
        _cachedModRepository = cachedModRepository;
        _localModRepository = localModRepository;
        _downloadService = downloadService;
        _fileSystemService = fileSystemService;
        _gameBananaClient = gameBananaClient;
        _steamLocator = steamLocator;
        _onNavigate = onNavigate;
        _onFilterHero = onFilterHero;
    }

    [RelayCommand]
    public async Task LoadHomeDataAsync()
    {
        if (_cachedModRepository == null || _localModRepository == null)
        {
            // Fallback preview data
            if (FeaturedMods.Count == 0)
            {
                FeaturedMods.Add(new ModCardViewModel { Name = "Health Color HUD", Author = "DeadModer", Description = "Настраиваемые цвета здоровья", Category = Core.Enums.ModCategory.UI, DownloadCountFormatted = "24.5k", Rating = 4.8 });
                FeaturedMods.Add(new ModCardViewModel { Name = "Better HUD Minimap", Author = "DeadModer", Description = "Чистый и компактный интерфейс", Category = Core.Enums.ModCategory.UI, DownloadCountFormatted = "12.4k", Rating = 4.8 });
                FeaturedMods.Add(new ModCardViewModel { Name = "Custom Crosshair Pro", Author = "DeadModer", Description = "Улучшенный прицел для всех героев", Category = Core.Enums.ModCategory.UI, DownloadCountFormatted = "8.1k", Rating = 4.7 });
            }
            TotalCatalogCount = 248;
            InstalledCount = 3;
            return;
        }

        try
        {
            var popular = await _cachedModRepository.GetPopularAsync(4);
            var installed = await _localModRepository.GetAllAsync();
            var allCached = await _cachedModRepository.GetAllAsync();

            var installedIds = new System.Collections.Generic.HashSet<string>(
                installed.Select(m => m.Id)
                    .Concat(installed.Where(m => !string.IsNullOrEmpty(m.RemoteModId)).Select(m => m.RemoteModId!)),
                StringComparer.OrdinalIgnoreCase);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalCatalogCount = allCached.Count > 0 ? allCached.Count : 240;
                InstalledCount = installed.Count;

                FeaturedMods.Clear();
                foreach (var m in popular.Take(4))
                {
                    if (_downloadService != null && _fileSystemService != null && _gameBananaClient != null)
                    {
                        var card = ModCardViewModel.FromCachedMod(
                            m,
                            installedIds.Contains(m.Id),
                            _downloadService,
                            _fileSystemService,
                            _localModRepository,
                            _gameBananaClient,
                            _ => { });
                        FeaturedMods.Add(card);
                    }
                }
            });
        }
        catch
        {
            // Silently fallback if loading takes time
        }
    }

    [RelayCommand]
    public void GoToCatalog()
    {
        (_onNavigate ?? OnNavigate)?.Invoke(NavigationTab.Mods);
    }

    [RelayCommand]
    public void GoToHeroes()
    {
        (_onNavigate ?? OnNavigate)?.Invoke(NavigationTab.Heroes);
    }

    [RelayCommand]
    public void GoToMyMods()
    {
        (_onNavigate ?? OnNavigate)?.Invoke(NavigationTab.MyMods);
    }

    [RelayCommand]
    public void FilterByHero(string heroName)
    {
        (_onFilterHero ?? OnFilterHero)?.Invoke(heroName);
    }

    [RelayCommand]
    public void LaunchGame()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "steam://run/1422450",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignored
        }
    }

    [RelayCommand]
    public async Task OpenAddonsFolderAsync()
    {
        try
        {
            if (_steamLocator != null)
            {
                var addonsPath = await _steamLocator.GetAddonsPathAsync();
                if (!string.IsNullOrEmpty(addonsPath) && System.IO.Directory.Exists(addonsPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = addonsPath,
                        UseShellExecute = true
                    });
                }
            }
        }
        catch
        {
            // Ignored
        }
    }
}
