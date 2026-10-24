using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeadModer.Core.Entities;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.UI.Services;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// Описание профиля героя для системы автоподбора баннера высокой чёткости.
/// </summary>
public record BannerHeroProfile(
    string Name,
    string Title,
    string Role,
    string ImageUri,
    string PrimaryColorHex,
    string SecondaryColorHex
);

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
    private readonly IDeadlockerScraper? _deadlockerScraper;
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

    /// <summary>
    /// Каталог высококачественных артов героев Deadlock с индивидуальной цветовой аурой Gilded Noir.
    /// </summary>
    private static readonly BannerHeroProfile[] HighQualityBannerHeroes =
    [
        new("Victor", "VICTOR — High Society Enforcer", "Assault", "avares://DeadModer.UI/Assets/Design/victor-cut.png", "#F0A93B", "#6B9BFF"),
        new("Mirage", "MIRAGE — The Djinn Wanderer", "Assault", "avares://DeadModer.UI/Assets/Heroes/mirage_full.png", "#F0A93B", "#FF5A3C"),
        new("Infernus", "INFERNUS — The Burning Soul", "Assault", "avares://DeadModer.UI/Assets/Heroes/inferno_full.png", "#FF5A3C", "#F0A93B"),
        new("Vindicta", "VINDICTA — The Spectral Sniper", "Assault", "avares://DeadModer.UI/Assets/Heroes/hornet_full.png", "#4FE0A8", "#FF5A3C"),
        new("Wraith", "WRAITH — The High Roller", "Assault", "avares://DeadModer.UI/Assets/Heroes/wraith_full.png", "#B184FF", "#FF5A3C"),
        new("Haze", "HAZE — The Silent Assassin", "Assault", "avares://DeadModer.UI/Assets/Heroes/haze_full.png", "#B184FF", "#F0A93B"),
        new("Seven", "SEVEN — Storm Incarnate", "Control", "avares://DeadModer.UI/Assets/Heroes/gigawatt_full.png", "#B184FF", "#6B9BFF"),
        new("Yamato", "YAMATO — The Shadow Ronin", "Assault", "avares://DeadModer.UI/Assets/Heroes/yamato_full.png", "#FF5A3C", "#EDE4D0"),
        new("Paradox", "PARADOX — Temporal Weaver", "Control", "avares://DeadModer.UI/Assets/Heroes/chrono_full.png", "#B184FF", "#F0A93B"),
        new("Abrams", "ABRAMS — The Arcane Detective", "Defense", "avares://DeadModer.UI/Assets/Heroes/bull_full.png", "#6B9BFF", "#F0A93B"),
        new("Kelvin", "KELVIN — Arctic Explorer", "Support", "avares://DeadModer.UI/Assets/Heroes/kelvin_full.png", "#6B9BFF", "#4FE0A8"),
        new("Viscous", "VISCOUS — The Sea Anomaly", "Support", "avares://DeadModer.UI/Assets/Heroes/viscous_full.png", "#4FE0A8", "#F0A93B"),
        new("Shiv", "SHIV — The Ruthless Brawler", "Assault", "avares://DeadModer.UI/Assets/Heroes/shiv_full.png", "#FF5A3C", "#F0A93B"),
        new("Bebop", "BEBOP — Heavy Scrap Golem", "Control", "avares://DeadModer.UI/Assets/Heroes/bebop_full.png", "#F0A93B", "#FF5A3C"),
        new("Grey Talon", "GREY TALON — Spirit Marksman", "Assault", "avares://DeadModer.UI/Assets/Heroes/archer_full.png", "#4FE0A8", "#F0A93B"),
        new("Warden", "WARDEN — Law Enforcer", "Defense", "avares://DeadModer.UI/Assets/Heroes/warden_full.png", "#6B9BFF", "#F0A93B"),
        new("Lady Geist", "LADY GEIST — Crimson Sorceress", "Assault", "avares://DeadModer.UI/Assets/Heroes/spectre_full.png", "#FF5A3C", "#B184FF"),
        new("Dynamo", "DYNAMO — Kinetic Singularity", "Support", "avares://DeadModer.UI/Assets/Heroes/engineer_full.png", "#6B9BFF", "#B184FF"),
        new("Pocket", "POCKET — Mystic Suitcase Vagrant", "Assault", "avares://DeadModer.UI/Assets/Heroes/astro_full.png", "#6B9BFF", "#4FE0A8"),
        new("Mo & Krill", "MO & KRILL — Underground Duo", "Defense", "avares://DeadModer.UI/Assets/Heroes/sumo_full.png", "#F0A93B", "#FF5A3C"),
        new("McGinnis", "MCGINNIS — Turret Architect", "Defense", "avares://DeadModer.UI/Assets/Heroes/digger_full.png", "#F0A93B", "#6B9BFF"),
        new("Lash", "LASH — The Arrogant Whipper", "Assault", "avares://DeadModer.UI/Assets/Heroes/tengu_full.png", "#FF5A3C", "#B184FF")
    ];

    [ObservableProperty]
    private Bitmap? _bannerHeroBitmap;

    [ObservableProperty]
    private string _bannerHeroDisplayName = "Victor";

    [ObservableProperty]
    private string _bannerHeroTitle = "VICTOR — High Society Enforcer";

    [ObservableProperty]
    private string _bannerHeroRole = "Assault";

    [ObservableProperty]
    private IBrush? _bannerAuraBrush;

    private int _lastHeroIndex = -1;

    public ObservableCollection<ModCardViewModel> FeaturedMods { get; } = [];

    public HomeViewModel() : this(null, null, null, null, null, null, null, null, null) { }

    public HomeViewModel(
        ICachedModRepository? cachedModRepository,
        ILocalModRepository? localModRepository,
        IDownloadService? downloadService,
        IFileSystemService? fileSystemService,
        IGameBananaClient? gameBananaClient,
        ISteamLocator? steamLocator,
        Action<NavigationTab>? onNavigate = null,
        Action<string>? onFilterHero = null,
        IDeadlockerScraper? deadlockerScraper = null)
    {
        _cachedModRepository = cachedModRepository;
        _localModRepository = localModRepository;
        _downloadService = downloadService;
        _fileSystemService = fileSystemService;
        _gameBananaClient = gameBananaClient;
        _steamLocator = steamLocator;
        _onNavigate = onNavigate;
        _onFilterHero = onFilterHero;
        _deadlockerScraper = deadlockerScraper;

        // Инициализируем начальный профиль героя для мгновенной отрисовки без задержек
        InitInitialBannerHero();
    }

    private void InitInitialBannerHero()
    {
        if (HighQualityBannerHeroes.Length == 0) return;

        // Выбираем случайного героя сразу при старте
        var idx = Random.Shared.Next(HighQualityBannerHeroes.Length);
        _lastHeroIndex = idx;
        var profile = HighQualityBannerHeroes[idx];

        BannerHeroDisplayName = profile.Name;
        BannerHeroTitle = profile.Title;
        BannerHeroRole = profile.Role;
        BannerAuraBrush = CreateAuraBrush(profile.PrimaryColorHex, profile.SecondaryColorHex);

        // Синхронная загрузка из локальных ресурсов для мгновенного появления в окне и previewer
        try
        {
            var uri = new Uri(profile.ImageUri);
            if (AssetLoader.Exists(uri))
            {
                using var stream = AssetLoader.Open(uri);
                BannerHeroBitmap = new Bitmap(stream);
            }
        }
        catch
        {
            // fallback к асинхронной загрузке
            _ = ApplyBannerHeroAsync(profile);
        }
    }

    private static IBrush CreateAuraBrush(string primaryHex, string secondaryHex)
    {
        try
        {
            var pColor = Color.Parse(primaryHex);
            var sColor = Color.Parse(secondaryHex);
            return new RadialGradientBrush
            {
                Center = RelativePoint.Center,
                GradientOrigin = RelativePoint.Center,
                GradientStops =
                [
                    new GradientStop(pColor, 0.0),
                    new GradientStop(sColor, 0.55),
                    new GradientStop(Colors.Transparent, 1.0)
                ]
            };
        }
        catch
        {
            return new RadialGradientBrush
            {
                GradientStops =
                [
                    new GradientStop(Color.Parse("#F0A93B"), 0.0),
                    new GradientStop(Colors.Transparent, 1.0)
                ]
            };
        }
    }

    /// <summary>
    /// Автоматический подбор случайного героя в высоком качестве с плавной сменой ауры.
    /// Гарантирует, что герой не повторяется два раза подряд.
    /// </summary>
    [RelayCommand]
    public async Task PickRandomBannerHeroAsync()
    {
        if (HighQualityBannerHeroes.Length == 0) return;

        int nextIdx;
        if (HighQualityBannerHeroes.Length > 1)
        {
            do
            {
                nextIdx = Random.Shared.Next(HighQualityBannerHeroes.Length);
            } while (nextIdx == _lastHeroIndex);
        }
        else
        {
            nextIdx = 0;
        }

        _lastHeroIndex = nextIdx;
        await ApplyBannerHeroAsync(HighQualityBannerHeroes[nextIdx]);
    }

    public async Task ApplyBannerHeroAsync(BannerHeroProfile profile)
    {
        BannerHeroDisplayName = profile.Name;
        BannerHeroTitle = profile.Title;
        BannerHeroRole = profile.Role;
        BannerAuraBrush = CreateAuraBrush(profile.PrimaryColorHex, profile.SecondaryColorHex);

        try
        {
            var bmp = await ImageCacheService.LoadBitmapAsync(profile.ImageUri);
            if (bmp != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() => BannerHeroBitmap = bmp);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HomeViewModel] Ошибка загрузки арта баннера: {ex.Message}");
        }
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
                    if (_downloadService != null && _fileSystemService != null)
                    {
                        var card = ModCardViewModel.FromCachedMod(
                            m,
                            installedIds.Contains(m.Id),
                            _downloadService,
                            _fileSystemService,
                            _localModRepository,
                            _gameBananaClient,
                            _ => { },
                            _deadlockerScraper);
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
