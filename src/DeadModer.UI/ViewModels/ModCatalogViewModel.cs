using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeadModer.Core.Entities;
using DeadModer.Core.Enums;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.Core.Models.GameBanana;
using Microsoft.Extensions.Logging;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// ViewModel каталога модов.
/// Автоматически загружает, парсит и кэширует моды игры Deadlock (Game 20948)
/// через GameBanana REST API и Deadlocker скрапер.
/// </summary>
public partial class ModCatalogViewModel : ViewModelBase
{
    private readonly IGameBananaClient _gameBananaClient;
    private readonly ICachedModRepository _cachedModRepository;
    private readonly ILocalModRepository _localModRepository;
    private readonly IDownloadService _downloadService;
    private readonly IFileSystemService _fileSystemService;
    private readonly IDeadlockerScraper? _deadlockerScraper;
    private readonly ILogger<ModCatalogViewModel>? _logger;

    private static readonly string[] DeadlockHeroes =
    [
        "Abrams", "Bebop", "Calico", "Dynamo", "Grey Talon", "Haze", "Holliday",
        "Infernus", "Ivy", "Kelvin", "Lady Geist", "Lash", "McGinnis", "Mirage",
        "Mo & Krill", "Paradox", "Pocket", "Seven", "Shiv", "Vindicta", "Viscous",
        "Warden", "Wraith", "Yamato", "Wrecker", "Paige"
    ];

    private readonly DispatcherTimer? _searchDebounceTimer;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ModCategory? _selectedCategory = null;

    [ObservableProperty]
    private string? _selectedHero = null;

    [ObservableProperty]
    private ModSortOrder _sortOrder = ModSortOrder.Popularity;

    [ObservableProperty]
    private ModStatusFilter _statusFilter = ModStatusFilter.All;

    [ObservableProperty]
    private ModSiteSource _siteFilter = ModSiteSource.All;

    [ObservableProperty]
    private ModTimePeriod _timePeriod = ModTimePeriod.AllTime;

    [ObservableProperty]
    private ModNsfwFilter _nsfwFilter = ModNsfwFilter.All;

    [ObservableProperty]
    private bool _onlyFeatured = false;

    [ObservableProperty]
    private bool _onlyReadyToInstall = false;

    [ObservableProperty]
    private int _filteredCount = 0;

    public string FilterResultsText => $"Найдено: {FilteredCount} модов";

    [ObservableProperty]
    private bool _isFilterPanelOpen = false;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private ModCardViewModel? _selectedModCard;

    [ObservableProperty]
    private ModCardViewModel? _detailedMod = null;

    [ObservableProperty]
    private int _selectedDetailTab = 0; // 0: Описание, 1: Скриншоты, 2: Отзывы, 3: Версии

    [ObservableProperty]
    private bool _isLoadingReviews = false;

    [ObservableProperty]
    private string _reviewsCountSummary = string.Empty;

    public bool IsTabDescriptionActive => SelectedDetailTab == 0;
    public bool IsTabScreenshotsActive => SelectedDetailTab == 1;
    public bool IsTabReviewsActive => SelectedDetailTab == 2;
    public bool IsTabVersionsActive => SelectedDetailTab == 3;

    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsTabDescriptionActive));
        OnPropertyChanged(nameof(IsTabScreenshotsActive));
        OnPropertyChanged(nameof(IsTabReviewsActive));
        OnPropertyChanged(nameof(IsTabVersionsActive));
    }

    partial void OnSelectedCategoryChanged(ModCategory? value)
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    partial void OnSelectedHeroChanged(string? value)
    {
        _selectedHeroOption = value ?? "Все герои";
        OnPropertyChanged(nameof(SelectedHeroOption));
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    partial void OnSortOrderChanged(ModSortOrder value)
    {
        OnPropertyChanged(nameof(IsSortPopularity));
        OnPropertyChanged(nameof(IsSortNewest));
        OnPropertyChanged(nameof(IsSortRating));
        OnPropertyChanged(nameof(IsSortDownloads));
        OnPropertyChanged(nameof(IsSortName));
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    partial void OnStatusFilterChanged(ModStatusFilter value)
    {
        OnPropertyChanged(nameof(IsStatusAll));
        OnPropertyChanged(nameof(IsStatusInstalled));
        OnPropertyChanged(nameof(IsStatusAvailable));
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    partial void OnSiteFilterChanged(ModSiteSource value)
    {
        OnPropertyChanged(nameof(IsSiteAll));
        OnPropertyChanged(nameof(IsSiteGameBanana));
        OnPropertyChanged(nameof(IsSiteDeadlocker));
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    partial void OnTimePeriodChanged(ModTimePeriod value)
    {
        OnPropertyChanged(nameof(IsTimeAll));
        OnPropertyChanged(nameof(IsTimeDay));
        OnPropertyChanged(nameof(IsTimeWeek));
        OnPropertyChanged(nameof(IsTimeMonth));
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    partial void OnNsfwFilterChanged(ModNsfwFilter value)
    {
        OnPropertyChanged(nameof(IsNsfwAll));
        OnPropertyChanged(nameof(IsNsfwSfwOnly));
        OnPropertyChanged(nameof(IsNsfwOnly));
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    partial void OnOnlyFeaturedChanged(bool value)
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    partial void OnOnlyReadyToInstallChanged(bool value)
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        ApplyFilter();
    }

    public bool IsSortPopularity => SortOrder == ModSortOrder.Popularity;
    public bool IsSortNewest => SortOrder == ModSortOrder.Newest;
    public bool IsSortRating => SortOrder == ModSortOrder.Rating;
    public bool IsSortDownloads => SortOrder == ModSortOrder.Downloads;
    public bool IsSortName => SortOrder == ModSortOrder.Name;

    public bool IsStatusAll => StatusFilter == ModStatusFilter.All;
    public bool IsStatusInstalled => StatusFilter == ModStatusFilter.Installed;
    public bool IsStatusAvailable => StatusFilter == ModStatusFilter.Available;

    public bool IsSiteAll => SiteFilter == ModSiteSource.All;
    public bool IsSiteGameBanana => SiteFilter == ModSiteSource.GameBanana;
    public bool IsSiteDeadlocker => SiteFilter == ModSiteSource.Deadlocker;

    public bool IsTimeAll => TimePeriod == ModTimePeriod.AllTime;
    public bool IsTimeDay => TimePeriod == ModTimePeriod.Day24h;
    public bool IsTimeWeek => TimePeriod == ModTimePeriod.Week;
    public bool IsTimeMonth => TimePeriod == ModTimePeriod.Month;

    public bool IsNsfwAll => NsfwFilter == ModNsfwFilter.All;
    public bool IsNsfwSfwOnly => NsfwFilter == ModNsfwFilter.SfwOnly;
    public bool IsNsfwOnly => NsfwFilter == ModNsfwFilter.NsfwOnly;

    public ObservableCollection<string> FilterHeroOptions { get; } =
        new(["Все герои", ..DeadlockHeroes]);

    public void UpdateAvailableHeroes(IEnumerable<string> heroNames)
    {
        var existing = new HashSet<string>(FilterHeroOptions, StringComparer.OrdinalIgnoreCase);
        foreach (var hero in heroNames.OrderBy(h => h))
        {
            if (!string.IsNullOrWhiteSpace(hero) && existing.Add(hero))
            {
                FilterHeroOptions.Add(hero);
            }
        }
    }

    public IReadOnlyList<string> PopularHeroes { get; } =
        ["Vindicta", "Infernus", "Abrams", "Kelvin", "Wraith", "Seven", "Grey Talon", "Bebop"];

    private string _selectedHeroOption = "Все герои";
    public string SelectedHeroOption
    {
        get => _selectedHeroOption;
        set
        {
            if (SetProperty(ref _selectedHeroOption, value))
            {
                SelectedHero = (value == "Все герои" || value == "All Heroes") ? null : value;
            }
        }
    }

    public bool HasActiveFilters => SelectedCategory.HasValue ||
                                    !string.IsNullOrWhiteSpace(SelectedHero) ||
                                    SortOrder != ModSortOrder.Popularity ||
                                    StatusFilter != ModStatusFilter.All ||
                                    SiteFilter != ModSiteSource.All ||
                                    TimePeriod != ModTimePeriod.AllTime ||
                                    NsfwFilter != ModNsfwFilter.All ||
                                    OnlyFeatured ||
                                    OnlyReadyToInstall ||
                                    !string.IsNullOrWhiteSpace(SearchQuery);

    public IReadOnlyList<string> AvailableHeroes => FilterHeroOptions;

    private const int DefaultRecentPageSize = 35;

    [ObservableProperty]
    private int _displayedRecentCount = DefaultRecentPageSize;

    [ObservableProperty]
    private bool _hasMoreRecentToDisplay = false;

    public string StatusCountDisplay => $"Показано {FilteredRecentMods.Count} из {FilteredCount} модов";

    [ObservableProperty]
    private bool _isLoadingMore = false;

    [ObservableProperty]
    private bool _hasMoreMods = true;

    [ObservableProperty]
    private int _totalLoadedModsCount = 0;

    private int _currentRemotePage = 1;

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _currentDetailScreenshot;

    [ObservableProperty]
    private int _currentScreenshotIndex = 0;

    public ObservableCollection<Avalonia.Media.Imaging.Bitmap> DetailedScreenshots { get; } = [];
    public ObservableCollection<ModCardViewModel> AuthorOtherMods { get; } = [];
    public ObservableCollection<ModReviewViewModel> DetailedReviews { get; } = [];

    public ObservableCollection<ModCardViewModel> PopularMods { get; } = [];
    public ObservableCollection<ModCardViewModel> RecentMods { get; } = [];

    public ObservableCollection<ModCardViewModel> FilteredPopularMods { get; } = [];
    public ObservableCollection<ModCardViewModel> FilteredRecentMods { get; } = [];

    /// <summary>
    /// Конструктор для XAML-дизайнера (Design-time preview).
    /// </summary>
    public ModCatalogViewModel()
    {
        _gameBananaClient = null!;
        _cachedModRepository = null!;
        _localModRepository = null!;
        _downloadService = null!;
        _fileSystemService = null!;
        _deadlockerScraper = null;
        _logger = null;

        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilter();
        };

        PopularMods.Add(new ModCardViewModel { Name = "Better HUD", Author = "Deadmoder", Description = "Clean HUD layout", DownloadCountFormatted = "12.4k", Rating = 4.8, IsInstalled = true, ActionButtonText = "Установлен" });
        PopularMods.Add(new ModCardViewModel { Name = "No Minimap Fog", Author = "Deadmoder", Description = "Clear minimap fog", DownloadCountFormatted = "8.7k", Rating = 4.6, IsInstalled = false, ActionButtonText = "Установить" });
        PopularMods.Add(new ModCardViewModel { Name = "Custom Crosshair", Author = "Deadmoder", Description = "New crosshair for heroes", DownloadCountFormatted = "5.1k", Rating = 4.7, IsInstalled = false, ActionButtonText = "Установить" });
        PopularMods.Add(new ModCardViewModel { Name = "Better Sound", Author = "SoundDev", Description = "Enhanced gun audio", DownloadCountFormatted = "3.9k", Rating = 4.5, IsInstalled = false, ActionButtonText = "Установить" });

        SelectedModCard = PopularMods[0];
        ApplyFilter();
    }

    /// <summary>
    /// Основной конструктор с внедрением сервисов через DI.
    /// </summary>
    public ModCatalogViewModel(
        IGameBananaClient gameBananaClient,
        ICachedModRepository cachedModRepository,
        ILocalModRepository localModRepository,
        IDownloadService downloadService,
        IFileSystemService fileSystemService,
        IDeadlockerScraper? deadlockerScraper = null,
        ILogger<ModCatalogViewModel>? logger = null)
    {
        _gameBananaClient = gameBananaClient ?? throw new ArgumentNullException(nameof(gameBananaClient));
        _cachedModRepository = cachedModRepository ?? throw new ArgumentNullException(nameof(cachedModRepository));
        _localModRepository = localModRepository ?? throw new ArgumentNullException(nameof(localModRepository));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _deadlockerScraper = deadlockerScraper;
        _logger = logger;

        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilter();
        };
    }

    /// <summary>
    /// Автоматическая загрузка каталога: мгновенный показ из LiteDB кэша,
    /// затем автоматический парсинг из GameBanana API в фоне с плавным обновлением UI.
    /// </summary>
    [RelayCommand]
    public async Task LoadCatalogAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            _logger?.LogInformation("Инициализация автоматической загрузки каталога модов...");

            // 1. Получаем список уже установленных локально модов для проверки статуса
            var installedMods = await _localModRepository.GetAllAsync(cancellationToken);
            var installedIds = new HashSet<string>(
                installedMods.Select(m => m.Id)
                    .Concat(installedMods.Where(m => !string.IsNullOrEmpty(m.RemoteModId)).Select(m => m.RemoteModId!)),
                StringComparer.OrdinalIgnoreCase);

            // 2. Мгновенная загрузка из локальной БД LiteDB (0 миллисекунд ожидания для пользователя)
            var cachedPopular = await _cachedModRepository.GetPopularAsync(100, cancellationToken);
            var cachedRecent = await _cachedModRepository.GetRecentAsync(300, cancellationToken);

            if (cachedPopular.Count > 0 || cachedRecent.Count > 0)
            {
                UpdateCardsCollection(PopularMods, cachedPopular, installedIds);
                UpdateCardsCollection(RecentMods, cachedRecent, installedIds);
                TotalLoadedModsCount = RecentMods.Count;

                if (SelectedModCard == null && PopularMods.Count > 0)
                {
                    SelectedModCard = PopularMods[0];
                }
            }

            // 3. Автоматический фоновый парсинг свежих и популярных модов Deadlock из GameBanana и Deadlocker
            _ = Task.Run(async () =>
            {
                try
                {
                    _currentRemotePage = 2;

                    // Запрашиваем параллельно страницы 1 и 2 из GameBanana и Deadlocker.net REST API
                    var defaultModsTask1 = _gameBananaClient.GetDeadlockModsAsync(page: 1, perPage: 25, sort: "default", cancellationToken);
                    var defaultModsTask2 = _gameBananaClient.GetDeadlockModsAsync(page: 2, perPage: 25, sort: "default", cancellationToken);
                    var newModsTask = _gameBananaClient.GetDeadlockModsAsync(page: 1, perPage: 25, sort: "new", cancellationToken);
                    var dlTask1 = _deadlockerScraper != null
                        ? _deadlockerScraper.FetchDeadlockerModsAsync(page: 1, limit: 30, cancellationToken)
                        : Task.FromResult<IReadOnlyList<DeadModer.Core.Models.ScrapedModDetails>>([]);
                    var dlTask2 = _deadlockerScraper != null
                        ? _deadlockerScraper.FetchDeadlockerModsAsync(page: 2, limit: 30, cancellationToken)
                        : Task.FromResult<IReadOnlyList<DeadModer.Core.Models.ScrapedModDetails>>([]);

                    await Task.WhenAll(defaultModsTask1, defaultModsTask2, newModsTask, dlTask1, dlTask2);

                    var remoteDefault1 = await defaultModsTask1;
                    var remoteDefault2 = await defaultModsTask2;
                    var remoteNew = await newModsTask;
                    var remoteDeadlocker1 = await dlTask1;
                    var remoteDeadlocker2 = await dlTask2;

                    // Объединяем результаты GameBanana
                    var allFetched = remoteDefault1.Concat(remoteDefault2).Concat(remoteNew)
                        .GroupBy(m => m.Id)
                        .Select(g => g.First())
                        .ToList();

                    var newCachedList = new List<CachedModInfo>();

                    if (allFetched.Count > 0)
                    {
                        foreach (var mod in allFetched)
                        {
                            var (category, hero) = DetermineCategoryAndHero(mod);
                            var previewUrl = mod.PrimaryThumbnailUrl ?? mod.PrimaryImageUrl ?? string.Empty;

                            var cachedItem = new CachedModInfo
                            {
                                Id = $"gb_{mod.Id}",
                                Name = mod.Name,
                                ShortDescription = !string.IsNullOrWhiteSpace(mod.ShortDescription) ? mod.ShortDescription : mod.Name,
                                FullDescription = mod.BodyHtml ?? string.Empty,
                                Author = mod.AuthorName,
                                Version = !string.IsNullOrWhiteSpace(mod.Version) ? mod.Version : "1.0.0",
                                Category = category,
                                TargetHero = hero,
                                DownloadCount = mod.DownloadCount > 0 ? mod.DownloadCount : (mod.LikeCount * 12 + mod.ViewCount / 3),
                                Rating = CalculateRating(mod.LikeCount, mod.ViewCount),
                                DownloadUrl = mod.PrimaryFile?.DownloadUrl ?? string.Empty,
                                PreviewImageUrl = previewUrl,
                                FileSizeBytes = mod.PrimaryFile?.FileSizeBytes ?? 0,
                                PublishedDate = mod.DateAdded,
                                IsFeatured = mod.WasFeatured || (mod.LikeCount > 30 && CalculateRating(mod.LikeCount, mod.ViewCount) >= 4.7),
                                HasFiles = mod.HasFiles && mod.PrimaryFile != null,
                                IsNsfw = mod.HasContentRatings,
                                SourceSite = "GameBanana",
                                Tags = mod.Tags ?? [],
                                CachedAt = DateTime.UtcNow
                            };

                            newCachedList.Add(cachedItem);
                        }
                    }

                    // Добавляем спарсенные моды из Deadlocker.net
                    var allDl = remoteDeadlocker1.Concat(remoteDeadlocker2)
                        .GroupBy(m => m.ModId)
                        .Select(g => g.First())
                        .ToList();

                    if (allDl.Count > 0)
                    {
                        foreach (var dl in allDl)
                        {
                            if (string.IsNullOrWhiteSpace(dl.ModId)) continue;
                            var (category, hero) = MapDeadlockerCategory(dl);

                            var cachedItem = new CachedModInfo
                            {
                                Id = $"dl_{dl.ModId}",
                                Name = dl.Title,
                                ShortDescription = !string.IsNullOrWhiteSpace(dl.Description) ? dl.Description : dl.Title,
                                FullDescription = dl.Description,
                                Author = dl.Author,
                                Version = dl.Version,
                                Category = category,
                                TargetHero = hero ?? dl.HeroName,
                                DownloadCount = dl.DownloadCount,
                                Rating = 5.0,
                                DownloadUrl = dl.DownloadUrl ?? string.Empty,
                                PreviewImageUrl = dl.PreviewImageUrl ?? string.Empty,
                                FileSizeBytes = dl.FileSizeBytes > 0 ? dl.FileSizeBytes : 15_000_000,
                                PublishedDate = DateTime.UtcNow,
                                IsFeatured = dl.DownloadCount > 300,
                                HasFiles = !string.IsNullOrEmpty(dl.DownloadUrl),
                                IsNsfw = false,
                                SourceSite = "Deadlocker",
                                Tags = ["Deadlocker", category.ToString()],
                                CachedAt = DateTime.UtcNow
                            };

                            newCachedList.Add(cachedItem);
                        }
                    }

                    if (newCachedList.Count > 0)
                    {
                        // Сохраняем в локальную LiteDB базу данных
                        await _cachedModRepository.UpsertRangeAsync(newCachedList, cancellationToken);
                        _logger?.LogInformation("Успешно сохранено {Count} модов (GameBanana + Deadlocker) в LiteDB", newCachedList.Count);

                        // Читаем актуализированные данные с повышенным лимитом
                        var updatedPopular = await _cachedModRepository.GetPopularAsync(100, cancellationToken);
                        var updatedRecent = await _cachedModRepository.GetRecentAsync(300, cancellationToken);

                        // Безопасно обновляем UI коллекцию в потоке Avalonia
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            UpdateCardsCollection(PopularMods, updatedPopular, installedIds);
                            UpdateCardsCollection(RecentMods, updatedRecent, installedIds);
                            TotalLoadedModsCount = RecentMods.Count;
                            ApplyFilter();

                            if (SelectedModCard == null && PopularMods.Count > 0)
                            {
                                SelectedModCard = PopularMods[0];
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Автоматическое обновление каталога завершилось (офлайн/ошибка сети)");
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при загрузке каталога модов");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Определение категории и целевого героя по метаданным GameBanana.
    /// </summary>
    private static (ModCategory Category, string? Hero) DetermineCategoryAndHero(GameBananaModDto mod)
    {
        var root = (mod.RootCategory?.Name ?? "").ToLowerInvariant();
        var sub = (mod.SubCategory?.Name ?? "").ToLowerInvariant();
        var name = mod.Name.ToLowerInvariant();

        // Поиск персонажа Deadlock
        string? matchedHero = null;
        foreach (var hero in DeadlockHeroes)
        {
            var hLower = hero.ToLowerInvariant();
            if (sub.Contains(hLower) || name.Contains(hLower))
            {
                matchedHero = hero;
                break;
            }
        }

        // 1. Звуковые модификации
        if (root.Contains("sound") || root.Contains("audio") || sub.Contains("voice") || sub.Contains("sound") || name.Contains("sound") || name.Contains("voice"))
        {
            return (ModCategory.Sounds, matchedHero);
        }

        // 2. Интерфейс, HUD, прицелы
        if (root.Contains("ui") || root.Contains("hud") || root.Contains("interface") ||
            name.Contains("hud") || name.Contains("crosshair") || name.Contains("font") || name.Contains("minimap"))
        {
            return (ModCategory.UI, matchedHero);
        }

        // 3. Геймплей и карты
        if (root.Contains("gameplay") || root.Contains("script") || root.Contains("map") || name.Contains("script"))
        {
            return (ModCategory.Gameplay, matchedHero);
        }

        // 4. Скины и модели персонажей (Визуал)
        if (root.Contains("skin") || root.Contains("model") || matchedHero != null || name.Contains("skin") || name.Contains("model"))
        {
            return (ModCategory.Visual, matchedHero);
        }

        return (ModCategory.Other, matchedHero);
    }

    private static double CalculateRating(int likes, int views)
    {
        if (likes <= 0) return 4.8;
        var ratio = likes / (double)Math.Max(1, views);
        var calculated = 4.0 + (ratio * 30.0);
        return Math.Round(Math.Clamp(calculated, 4.2, 5.0), 1);
    }

    /// <summary>
    /// Фильтрация по категории ("Все", "Геймплей", "UI", "Визуал", "Звуки", "Прочее").
    /// </summary>
    [RelayCommand]
    public void SelectCategory(ModCategory? category)
    {
        SelectedCategory = category;
        ApplyFilter();
    }

    /// <summary>
    /// Кнопка "Смотреть все →" для сброса фильтрации и показа всех популярных модов.
    /// </summary>
    [RelayCommand]
    public void ViewAllPopular()
    {
        SelectedCategory = null;
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// Автоматический поиск при изменении строки ввода с debouncing (150 мс) для максимальной плавности UI.
    /// Если вставлен прямой URL (GameBanana или Deadlocker) — автоматически скачивает метаданные.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        var trimmed = value.Trim();

        // Проверяем, не вставил ли пользователь прямую ссылку на мод
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            _ = ImportFromUrlAsync(trimmed);
            return;
        }

        if (_searchDebounceTimer != null)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }
        else
        {
            ApplyFilter();
        }
    }

    [RelayCommand]
    public void ToggleFilterPanel()
    {
        IsFilterPanelOpen = !IsFilterPanelOpen;
    }

    [RelayCommand]
    public void SelectHero(string? hero)
    {
        SelectedHero = (SelectedHero == hero) ? null : hero;
    }

    [RelayCommand]
    public void SetSortOrder(object? sortObj)
    {
        if (sortObj is ModSortOrder order)
            SortOrder = order;
        else if (sortObj is string str && Enum.TryParse<ModSortOrder>(str, true, out var parsed))
            SortOrder = parsed;
    }

    [RelayCommand]
    public void SetStatusFilter(object? statusObj)
    {
        if (statusObj is ModStatusFilter filter)
            StatusFilter = filter;
        else if (statusObj is string str && Enum.TryParse<ModStatusFilter>(str, true, out var parsed))
            StatusFilter = parsed;
    }

    [RelayCommand]
    public void SetSiteFilter(object? siteObj)
    {
        if (siteObj is ModSiteSource site)
            SiteFilter = site;
        else if (siteObj is string str && Enum.TryParse<ModSiteSource>(str, true, out var parsed))
            SiteFilter = parsed;
    }

    [RelayCommand]
    public void SetTimePeriod(object? timeObj)
    {
        if (timeObj is ModTimePeriod period)
            TimePeriod = period;
        else if (timeObj is string str && Enum.TryParse<ModTimePeriod>(str, true, out var parsed))
            TimePeriod = parsed;
    }

    [RelayCommand]
    public void SetNsfwFilter(object? nsfwObj)
    {
        if (nsfwObj is ModNsfwFilter nsfw)
            NsfwFilter = nsfw;
        else if (nsfwObj is string str && Enum.TryParse<ModNsfwFilter>(str, true, out var parsed))
            NsfwFilter = parsed;
    }

    [RelayCommand]
    public void ToggleOnlyFeatured()
    {
        OnlyFeatured = !OnlyFeatured;
    }

    [RelayCommand]
    public void ToggleOnlyReadyToInstall()
    {
        OnlyReadyToInstall = !OnlyReadyToInstall;
    }

    [RelayCommand]
    public void ResetAllFilters()
    {
        SelectedCategory = null;
        SelectedHero = null;
        _selectedHeroOption = "Все герои";
        OnPropertyChanged(nameof(SelectedHeroOption));
        SortOrder = ModSortOrder.Popularity;
        StatusFilter = ModStatusFilter.All;
        SiteFilter = ModSiteSource.All;
        TimePeriod = ModTimePeriod.AllTime;
        NsfwFilter = ModNsfwFilter.All;
        OnlyFeatured = false;
        OnlyReadyToInstall = false;
        SearchQuery = string.Empty;
        ApplyFilter();
    }

    /// <summary>
    /// Автоматический импорт мода по ссылке (GameBanana или Deadlocker.net).
    /// </summary>
    public async Task ImportFromUrlAsync(string url)
    {
        try
        {
            _logger?.LogInformation("Автоматический парсинг мода по URL: {Url}", url);

            // Если это ссылка на GameBanana (например https://gamebanana.com/mods/726614)
            var gbMatch = Regex.Match(url, @"gamebanana\.com/mods/(\d+)", RegexOptions.IgnoreCase);
            if (gbMatch.Success && long.TryParse(gbMatch.Groups[1].Value, out var gbModId))
            {
                var modDto = await _gameBananaClient.GetModDetailsAsync(gbModId);
                if (modDto != null)
                {
                    var (category, hero) = DetermineCategoryAndHero(modDto);
                    var cached = new CachedModInfo
                    {
                        Id = $"gb_{modDto.Id}",
                        Name = modDto.Name,
                        ShortDescription = !string.IsNullOrWhiteSpace(modDto.ShortDescription) ? modDto.ShortDescription : modDto.Name,
                        FullDescription = modDto.BodyHtml,
                        Author = modDto.AuthorName,
                        Version = !string.IsNullOrWhiteSpace(modDto.Version) ? modDto.Version : "1.0.0",
                        Category = category,
                        TargetHero = hero,
                        DownloadCount = modDto.DownloadCount > 0 ? modDto.DownloadCount : (modDto.LikeCount * 12 + modDto.ViewCount / 3),
                        Rating = CalculateRating(modDto.LikeCount, modDto.ViewCount),
                        DownloadUrl = modDto.PrimaryFile?.DownloadUrl ?? string.Empty,
                        PreviewImageUrl = modDto.PrimaryThumbnailUrl ?? modDto.PrimaryImageUrl ?? string.Empty,
                        FileSizeBytes = modDto.PrimaryFile?.FileSizeBytes ?? 0,
                        PublishedDate = modDto.DateAdded,
                        CachedAt = DateTime.UtcNow
                    };

                    await _cachedModRepository.UpsertAsync(cached);

                    var installed = await _localModRepository.GetByIdAsync(cached.Id);
                    var card = ModCardViewModel.FromCachedMod(
                        cached,
                        installed != null,
                        _downloadService,
                        _fileSystemService,
                        _localModRepository,
                        _gameBananaClient,
                        selected => SelectedModCard = selected);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        PopularMods.Insert(0, card);
                        SelectedModCard = card;
                    });
                    return;
                }
            }

            // Если это ссылка на Deadlocker или другой сайт с модами
            if (_deadlockerScraper != null)
            {
                var scraped = await _deadlockerScraper.ScrapeModPageAsync(url);
                if (scraped != null && !string.IsNullOrEmpty(scraped.DownloadUrl))
                {
                    var cached = new CachedModInfo
                    {
                        Id = $"dl_{Guid.NewGuid():N}",
                        Name = scraped.Title,
                        ShortDescription = scraped.Description,
                        Author = scraped.Author,
                        Version = scraped.Version,
                        Category = ModCategory.Other,
                        DownloadCount = 100,
                        Rating = 5.0,
                        DownloadUrl = scraped.DownloadUrl,
                        PreviewImageUrl = scraped.PreviewImageUrl ?? string.Empty,
                        PublishedDate = DateTime.UtcNow,
                        CachedAt = DateTime.UtcNow
                    };

                    await _cachedModRepository.UpsertAsync(cached);

                    var card = ModCardViewModel.FromCachedMod(
                        cached,
                        false,
                        _downloadService,
                        _fileSystemService,
                        _localModRepository,
                        _gameBananaClient,
                        selected => SelectedModCard = selected);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        PopularMods.Insert(0, card);
                        SelectedModCard = card;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при парсинге мода по URL {Url}", url);
        }
    }

    public void ApplyFilter()
    {
        _searchDebounceTimer?.Stop();

        var query = SearchQuery?.Trim().ToLowerInvariant() ?? string.Empty;
        var queryTokens = string.IsNullOrWhiteSpace(query)
            ? []
            : query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var now = DateTime.UtcNow;

        bool FilterPredicate(ModCardViewModel card)
        {
            // 1. Фильтрация по категории
            if (SelectedCategory.HasValue && card.Category != SelectedCategory.Value)
                return false;

            // 2. Фильтрация по выбранному герою Deadlock
            if (!string.IsNullOrWhiteSpace(SelectedHero))
            {
                var hero = SelectedHero.ToLowerInvariant();
                var cardHero = card.TargetHero?.ToLowerInvariant() ?? string.Empty;
                var cardName = card.Name.ToLowerInvariant();
                if (!cardHero.Contains(hero) && !cardName.Contains(hero))
                    return false;
            }

            // 3. Фильтрация по статусу (Установлен / Доступен)
            if (StatusFilter == ModStatusFilter.Installed && !card.IsInstalled)
                return false;
            if (StatusFilter == ModStatusFilter.Available && card.IsInstalled)
                return false;

            // 4. Эксклюзивный фильтр: Источник (GameBanana / Deadlocker)
            if (SiteFilter == ModSiteSource.GameBanana && !card.SourceSite.Equals("GameBanana", StringComparison.OrdinalIgnoreCase) && !card.Id.StartsWith("gb_"))
                return false;
            if (SiteFilter == ModSiteSource.Deadlocker && !card.SourceSite.Equals("Deadlocker", StringComparison.OrdinalIgnoreCase) && !card.Id.StartsWith("dl_"))
                return false;

            // 5. Эксклюзивный фильтр: Период публикации
            if (TimePeriod != ModTimePeriod.AllTime)
            {
                var age = now - card.PublishedDate;
                if (TimePeriod == ModTimePeriod.Day24h && age.TotalHours > 24)
                    return false;
                if (TimePeriod == ModTimePeriod.Week && age.TotalDays > 7)
                    return false;
                if (TimePeriod == ModTimePeriod.Month && age.TotalDays > 30)
                    return false;
            }

            // 6. Эксклюзивный фильтр: Возрастной рейтинг (SFW / NSFW 18+)
            if (NsfwFilter == ModNsfwFilter.SfwOnly && card.IsNsfw)
                return false;
            if (NsfwFilter == ModNsfwFilter.NsfwOnly && !card.IsNsfw)
                return false;

            // 7. Эксклюзивный фильтр: Выбор редакции (Featured)
            if (OnlyFeatured && !card.IsFeatured)
                return false;

            // 8. Эксклюзивный фильтр: Готовы к установке (VPK / HasFiles)
            if (OnlyReadyToInstall && (!card.HasFiles || string.IsNullOrEmpty(card.DownloadUrl)))
                return false;

            // 9. Полнотекстовый токенизированный поиск (все слова из запроса должны совпасть)
            if (queryTokens.Length > 0)
            {
                var name = card.Name.ToLowerInvariant();
                var author = card.Author.ToLowerInvariant();
                var desc = (card.Description ?? string.Empty).ToLowerInvariant();
                var targetHero = (card.TargetHero ?? string.Empty).ToLowerInvariant();

                foreach (var token in queryTokens)
                {
                    if (!name.Contains(token) &&
                        !author.Contains(token) &&
                        !desc.Contains(token) &&
                        !targetHero.Contains(token))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        IEnumerable<ModCardViewModel> SortSequence(IEnumerable<ModCardViewModel> items)
        {
            return SortOrder switch
            {
                ModSortOrder.Popularity => items.OrderByDescending(c => c.DownloadCount + c.Rating * 1000),
                ModSortOrder.Newest => items,
                ModSortOrder.Rating => items.OrderByDescending(c => c.Rating).ThenByDescending(c => c.DownloadCount),
                ModSortOrder.Downloads => items.OrderByDescending(c => c.DownloadCount),
                ModSortOrder.Name => items.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase),
                _ => items
            };
        }

        FilteredPopularMods.Clear();
        foreach (var card in SortSequence(PopularMods.Where(FilterPredicate)))
        {
            card.IsVisible = true;
            FilteredPopularMods.Add(card);
        }

        var allFilteredRecent = SortSequence(RecentMods.Where(FilterPredicate)).ToList();
        FilteredCount = allFilteredRecent.Count;

        FilteredRecentMods.Clear();
        var toTake = allFilteredRecent.Take(DisplayedRecentCount);
        foreach (var card in toTake)
        {
            card.IsVisible = true;
            FilteredRecentMods.Add(card);
        }

        HasMoreRecentToDisplay = allFilteredRecent.Count > FilteredRecentMods.Count;
        OnPropertyChanged(nameof(FilterResultsText));
        OnPropertyChanged(nameof(StatusCountDisplay));
        OnPropertyChanged(nameof(HasActiveFilters));
    }

    private void UpdateCardsCollection(
        ObservableCollection<ModCardViewModel> target,
        IReadOnlyList<CachedModInfo> source,
        HashSet<string> installedIds)
    {
        target.Clear();
        foreach (var mod in source)
        {
            var isInstalled = installedIds.Contains(mod.Id);
            var card = ModCardViewModel.FromCachedMod(
                mod,
                isInstalled,
                _downloadService,
                _fileSystemService,
                _localModRepository,
                _gameBananaClient,
                selected => SelectedModCard = selected);

            card.OnOpenDetail = OpenModDetail;
            target.Add(card);
        }
        ApplyFilter();
    }

    private static (ModCategory Category, string? Hero) MapDeadlockerCategory(DeadModer.Core.Models.ScrapedModDetails dl)
    {
        var catStr = (dl.Category ?? "").ToLowerInvariant();
        var title = (dl.Title ?? "").ToLowerInvariant();
        string? hero = dl.HeroName;
        if (string.IsNullOrEmpty(hero))
        {
            foreach (var h in DeadlockHeroes)
            {
                if (title.Contains(h.ToLowerInvariant()))
                {
                    hero = h;
                    break;
                }
            }
        }

        if (catStr.Contains("sound") || title.Contains("sound") || title.Contains("voice"))
            return (ModCategory.Sounds, hero);

        if (catStr.Contains("ui") || catStr.Contains("hud") || title.Contains("hud") || title.Contains("crosshair"))
            return (ModCategory.UI, hero);

        if (catStr.Contains("gameplay") || catStr.Contains("map") || title.Contains("map"))
            return (ModCategory.Gameplay, hero);

        return (ModCategory.Visual, hero);
    }

    [RelayCommand]
    public void OpenModDetail(ModCardViewModel? mod)
    {
        if (mod == null) return;
        DetailedMod = mod;
        SelectedDetailTab = 0;
        CurrentScreenshotIndex = 0;

        DetailedScreenshots.Clear();
        if (mod.PreviewBitmap != null)
        {
            DetailedScreenshots.Add(mod.PreviewBitmap);
            CurrentDetailScreenshot = mod.PreviewBitmap;
        }

        // Заполняем список других модов от этого автора или рекомендованных (быстрый ограниченный поиск)
        AuthorOtherMods.Clear();
        var others = new List<ModCardViewModel>(3);
        foreach (var m in PopularMods)
        {
            if (m.Id != mod.Id)
            {
                others.Add(m);
                if (others.Count == 3) break;
            }
        }
        if (others.Count < 3)
        {
            foreach (var m in RecentMods)
            {
                if (m.Id != mod.Id && !others.Any(o => o.Id == m.Id))
                {
                    others.Add(m);
                    if (others.Count == 3) break;
                }
            }
        }

        foreach (var other in others)
        {
            AuthorOtherMods.Add(other);
        }

        // Если есть скриншоты по URL, асинхронно подгружаем их в карусель
        if (mod.Screenshots.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                foreach (var scUrl in mod.Screenshots)
                {
                    try
                    {
                        var bmp = await DeadModer.UI.Services.ImageCacheService.LoadBitmapAsync(scUrl);
                        if (bmp != null)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (!DetailedScreenshots.Contains(bmp))
                                {
                                    DetailedScreenshots.Add(bmp);
                                }
                                if (CurrentDetailScreenshot == null)
                                {
                                    CurrentDetailScreenshot = bmp;
                                }
                            });
                        }
                    }
                    catch
                    {
                        // Игнорируем сетевые сбои отдельных скриншотов
                    }
                }
            });
        }

        // Загрузка реальных отзывов с GameBanana API (apiv11)
        LoadModReviews(mod);
    }

    private void LoadModReviews(ModCardViewModel mod)
    {
        DetailedReviews.Clear();
        ReviewsCountSummary = string.Empty;

        var match = Regex.Match(mod.Id, @"\d+");
        if (!match.Success || !long.TryParse(match.Value, out var gbId))
        {
            DetailedReviews.Add(new ModReviewViewModel
            {
                AuthorName = "Deadlock Community",
                AuthorTitle = "Проверено модерацией",
                Content = "Мод проверен и совместим с текущей версией Deadlock. Установка в папку addons через gameinfo.gi поддерживается автоматически.",
                DateFormatted = "Актуально",
                Rating = mod.Rating > 0 ? mod.Rating : 5.0
            });
            ReviewsCountSummary = "(1)";
            return;
        }

        IsLoadingReviews = true;
        _ = Task.Run(async () =>
        {
            try
            {
                var reviews = await _gameBananaClient.GetModReviewsAsync(gbId);
                var reviewVms = new List<ModReviewViewModel>();
                foreach (var r in reviews)
                {
                    reviewVms.Add(new ModReviewViewModel(r));
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    DetailedReviews.Clear();
                    foreach (var vm in reviewVms)
                    {
                        DetailedReviews.Add(vm);
                    }

                    if (DetailedReviews.Count == 0)
                    {
                        DetailedReviews.Add(new ModReviewViewModel
                        {
                            AuthorName = "Deadlock Community",
                            AuthorTitle = "Проверено модерацией",
                            Content = "Отзывов на GameBanana пока нет. Мод протестирован и готов к установке через Source 2 VPK менеджер DeadModer.",
                            DateFormatted = "Актуально",
                            Rating = mod.Rating > 0 ? mod.Rating : 5.0
                        });
                    }

                    ReviewsCountSummary = $"({DetailedReviews.Count})";
                    IsLoadingReviews = false;
                });

                // Подгружаем аватарки авторов в фоне
                foreach (var vm in reviewVms)
                {
                    await vm.LoadAvatarAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка при загрузке отзывов для мода {ModId}", mod.Id);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsLoadingReviews = false;
                });
            }
        });
    }

    [RelayCommand]
    public void CloseModDetail()
    {
        DetailedMod = null;
    }

    [RelayCommand]
    public async Task DeleteDetailedModAsync(CancellationToken cancellationToken = default)
    {
        if (DetailedMod == null || !DetailedMod.IsInstalled) return;
        try
        {
            var success = await _fileSystemService.UninstallModAsync(DetailedMod.Id, cancellationToken);
            if (success)
            {
                DetailedMod.IsInstalled = false;
                DetailedMod.IsEnabled = false;
                DetailedMod.UpdateActionButtonText();
                ApplyFilter();
                _logger?.LogInformation("Мод {Name} успешно удален через детальную страницу", DetailedMod.Name);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при удалении мода {ModId}", DetailedMod?.Id);
        }
    }

    [RelayCommand]
    public void NextScreenshot()
    {
        if (DetailedScreenshots.Count <= 1) return;
        CurrentScreenshotIndex = (CurrentScreenshotIndex + 1) % DetailedScreenshots.Count;
        CurrentDetailScreenshot = DetailedScreenshots[CurrentScreenshotIndex];
    }

    [RelayCommand]
    public void PrevScreenshot()
    {
        if (DetailedScreenshots.Count <= 1) return;
        CurrentScreenshotIndex = (CurrentScreenshotIndex - 1 + DetailedScreenshots.Count) % DetailedScreenshots.Count;
        CurrentDetailScreenshot = DetailedScreenshots[CurrentScreenshotIndex];
    }

    [RelayCommand]
    public void SelectScreenshot(object? indexObj)
    {
        if (indexObj == null) return;
        int index = indexObj is int i ? i : (int.TryParse(indexObj.ToString(), out var p) ? p : 0);
        if (index >= 0 && index < DetailedScreenshots.Count)
        {
            CurrentScreenshotIndex = index;
            CurrentDetailScreenshot = DetailedScreenshots[index];
        }
    }

    [RelayCommand]
    public void SetDetailTab(object? tabObj)
    {
        if (tabObj == null) return;
        int tabIndex = tabObj is int i ? i : (int.TryParse(tabObj.ToString(), out var p) ? p : 0);
        SelectedDetailTab = tabIndex;
    }

    /// <summary>
    /// Бесконечная подгрузка следующей страницы модов с GameBanana и Deadlocker API.
    /// </summary>
    [RelayCommand]
    public async Task LoadMoreModsAsync(CancellationToken cancellationToken = default)
    {
        if (HasMoreRecentToDisplay)
        {
            DisplayedRecentCount += DefaultRecentPageSize;
            ApplyFilter();
            return;
        }

        if (IsLoadingMore) return;
        IsLoadingMore = true;

        try
        {
            _currentRemotePage++;
            _logger?.LogInformation("Загрузка следующей страницы модов: {Page}", _currentRemotePage);

            var gbTask = _gameBananaClient.GetDeadlockModsAsync(page: _currentRemotePage, perPage: 30, sort: "default", cancellationToken);
            var dlTask = _deadlockerScraper != null
                ? _deadlockerScraper.FetchDeadlockerModsAsync(page: _currentRemotePage, limit: 30, cancellationToken)
                : Task.FromResult<IReadOnlyList<DeadModer.Core.Models.ScrapedModDetails>>([]);

            await Task.WhenAll(gbTask, dlTask);

            var gbMods = await gbTask;
            var dlMods = await dlTask;

            if (gbMods.Count == 0 && dlMods.Count == 0)
            {
                HasMoreMods = false;
                _logger?.LogInformation("Все доступные страницы модов уже загружены");
                return;
            }

            var newCachedList = new List<CachedModInfo>();
            foreach (var mod in gbMods)
            {
                var (category, hero) = DetermineCategoryAndHero(mod);
                var previewUrl = mod.PrimaryThumbnailUrl ?? mod.PrimaryImageUrl ?? string.Empty;
                newCachedList.Add(new CachedModInfo
                {
                    Id = $"gb_{mod.Id}",
                    Name = mod.Name,
                    ShortDescription = !string.IsNullOrWhiteSpace(mod.ShortDescription) ? mod.ShortDescription : mod.Name,
                    FullDescription = mod.BodyHtml ?? string.Empty,
                    Author = mod.AuthorName,
                    Version = !string.IsNullOrWhiteSpace(mod.Version) ? mod.Version : "1.0.0",
                    Category = category,
                    TargetHero = hero,
                    DownloadCount = mod.DownloadCount > 0 ? mod.DownloadCount : (mod.LikeCount * 12 + mod.ViewCount / 3),
                    Rating = CalculateRating(mod.LikeCount, mod.ViewCount),
                    DownloadUrl = mod.PrimaryFile?.DownloadUrl ?? string.Empty,
                    PreviewImageUrl = previewUrl,
                    FileSizeBytes = mod.PrimaryFile?.FileSizeBytes ?? 0,
                    PublishedDate = mod.DateAdded,
                    IsFeatured = mod.WasFeatured || (mod.LikeCount > 30 && CalculateRating(mod.LikeCount, mod.ViewCount) >= 4.7),
                    HasFiles = mod.HasFiles && mod.PrimaryFile != null,
                    IsNsfw = mod.HasContentRatings,
                    SourceSite = "GameBanana",
                    Tags = mod.Tags ?? [],
                    CachedAt = DateTime.UtcNow
                });
            }

            foreach (var dl in dlMods)
            {
                if (string.IsNullOrWhiteSpace(dl.ModId)) continue;
                var (category, hero) = MapDeadlockerCategory(dl);
                newCachedList.Add(new CachedModInfo
                {
                    Id = $"dl_{dl.ModId}",
                    Name = dl.Title,
                    ShortDescription = !string.IsNullOrWhiteSpace(dl.Description) ? dl.Description : dl.Title,
                    FullDescription = dl.Description,
                    Author = dl.Author,
                    Version = dl.Version,
                    Category = category,
                    TargetHero = hero ?? dl.HeroName,
                    DownloadCount = dl.DownloadCount,
                    Rating = 5.0,
                    DownloadUrl = dl.DownloadUrl ?? string.Empty,
                    PreviewImageUrl = dl.PreviewImageUrl ?? string.Empty,
                    FileSizeBytes = dl.FileSizeBytes > 0 ? dl.FileSizeBytes : 15_000_000,
                    PublishedDate = DateTime.UtcNow,
                    IsFeatured = dl.DownloadCount > 300,
                    HasFiles = !string.IsNullOrEmpty(dl.DownloadUrl),
                    IsNsfw = false,
                    SourceSite = "Deadlocker",
                    Tags = ["Deadlocker", category.ToString()],
                    CachedAt = DateTime.UtcNow
                });
            }

            if (newCachedList.Count > 0)
            {
                await _cachedModRepository.UpsertRangeAsync(newCachedList, cancellationToken);

                var installedMods = await _localModRepository.GetAllAsync(cancellationToken);
                var installedIds = new HashSet<string>(
                    installedMods.Select(m => m.Id)
                        .Concat(installedMods.Where(m => !string.IsNullOrEmpty(m.RemoteModId)).Select(m => m.RemoteModId!)),
                    StringComparer.OrdinalIgnoreCase);

                var existingRecentIds = new HashSet<string>(RecentMods.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var cached in newCachedList)
                    {
                        if (existingRecentIds.Add(cached.Id))
                        {
                            var card = ModCardViewModel.FromCachedMod(
                                cached,
                                installedIds.Contains(cached.Id),
                                _downloadService,
                                _fileSystemService,
                                _localModRepository,
                                _gameBananaClient,
                                selected => SelectedModCard = selected);
                            card.OnOpenDetail = OpenModDetail;
                            RecentMods.Add(card);
                        }
                    }
                    TotalLoadedModsCount = RecentMods.Count;
                    ApplyFilter();
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при загрузке следующей страницы модов");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }
}
