using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeadModer.Core.Entities;
using DeadModer.Core.Enums;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.Core.Models;
using DeadModer.UI.Services;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// ViewModel отдельной карточки мода в каталоге или списке установленных.
/// </summary>
public partial class ModCardViewModel : ViewModelBase
{
    private readonly IDownloadService _downloadService;
    private readonly IFileSystemService _fileSystemService;
    private readonly ILocalModRepository _modRepository;
    private readonly IGameBananaClient? _gameBananaClient;
    private readonly Action<ModCardViewModel>? _onSelected;

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = "Deadmoder";
    public string Version { get; init; } = "1.0.0";
    public ModCategory Category { get; init; } = ModCategory.Other;
    public string? TargetHero { get; init; }
    public int DownloadCount { get; init; }
    public string DownloadCountFormatted { get; init; } = "0";
    public double Rating { get; init; } = 5.0;
    public long FileSizeBytes { get; init; }
    public string FormattedSize { get; init; } = "0 MB";
    public string? PreviewImageUrl { get; init; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string DateFormatted { get; init; } = DateTime.UtcNow.ToString("dd.MM.yyyy");

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private double _installProgress = 0.0;

    [ObservableProperty]
    private string _actionButtonText = "Установить";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isSelected = false;

    [ObservableProperty]
    private bool _isFavorite = false;

    [ObservableProperty]
    private string _longDescription = string.Empty;

    public ObservableCollection<string> Tags { get; } = [];
    public ObservableCollection<string> Screenshots { get; } = [];
    public ObservableCollection<string> WhatChanges { get; } = [];

    public string AuthorLine => $"{Category} • Автор: {Author} • {FormattedSize} • v{Version}";
    public string RatingFormatted => $"★ {Rating:F1} ({DownloadCountFormatted} отзывов)";

    [ObservableProperty]
    private Avalonia.Media.Imaging.Bitmap? _previewBitmap;

    [ObservableProperty]
    private bool _isFeatured = false;

    [ObservableProperty]
    private bool _hasFiles = true;

    [ObservableProperty]
    private bool _isNsfw = false;

    [ObservableProperty]
    private string _sourceSite = "GameBanana";

    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;

    public ModCardViewModel()
    {
        _downloadService = null!;
        _fileSystemService = null!;
        _modRepository = null!;
        _gameBananaClient = null;
        Name = "Better HUD";
        Author = "Deadmoder";
        Description = "Clean HUD layout";
        DownloadCountFormatted = "12.4k";
        Rating = 4.8;
        FormattedSize = "12.4 MB";
        IsInstalled = true;
        IsEnabled = true;
        UpdateActionButtonText();
        Loc.LanguageChanged += UpdateActionButtonText;
    }

    public ModCardViewModel(
        IDownloadService downloadService,
        IFileSystemService fileSystemService,
        ILocalModRepository modRepository,
        IGameBananaClient? gameBananaClient = null,
        Action<ModCardViewModel>? onSelected = null)
    {
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _modRepository = modRepository ?? throw new ArgumentNullException(nameof(modRepository));
        _gameBananaClient = gameBananaClient;
        _onSelected = onSelected;
        Loc.LanguageChanged += UpdateActionButtonText;
    }

    public void UpdateActionButtonText()
    {
        if (IsInstalling)
        {
            ActionButtonText = $"{Math.Round(InstallProgress * 100)}%";
        }
        else if (IsInstalled)
        {
            ActionButtonText = IsEnabled ? Loc["Btn_Enabled"] : Loc["Btn_Disabled"];
        }
        else
        {
            ActionButtonText = Loc["Btn_Install"];
        }
    }

    public Action<ModCardViewModel>? OnOpenDetail { get; set; }

    public IDeadlockerScraper? DeadlockerScraper { get; set; }

    public static ModCardViewModel FromCachedMod(
        CachedModInfo cached,
        bool isInstalled,
        IDownloadService downloadService,
        IFileSystemService fileSystemService,
        ILocalModRepository modRepository,
        IGameBananaClient? gameBananaClient = null,
        Action<ModCardViewModel>? onSelected = null,
        IDeadlockerScraper? deadlockerScraper = null)
    {
        var site = !string.IsNullOrWhiteSpace(cached.SourceSite)
            ? cached.SourceSite
            : (cached.Id.StartsWith("dl_") ? "Deadlocker" : "GameBanana");

        var hasFiles = cached.HasFiles || cached.Id.StartsWith("dl_") || !string.IsNullOrEmpty(cached.DownloadUrl);

        var vm = new ModCardViewModel(downloadService, fileSystemService, modRepository, gameBananaClient, onSelected)
        {
            Id = cached.Id,
            Name = cached.Name,
            Description = !string.IsNullOrEmpty(cached.ShortDescription) ? cached.ShortDescription : cached.Name,
            Author = cached.Author,
            Version = cached.Version,
            Category = cached.Category,
            TargetHero = cached.TargetHero,
            DownloadCount = cached.DownloadCount,
            DownloadCountFormatted = cached.FormattedDownloads,
            Rating = cached.Rating,
            FileSizeBytes = cached.FileSizeBytes,
            FormattedSize = cached.FormattedSize,
            PreviewImageUrl = cached.PreviewImageUrl,
            DownloadUrl = cached.DownloadUrl,
            DateFormatted = cached.PublishedDate.ToString("dd.MM.yyyy"),
            PublishedDate = cached.PublishedDate,
            IsFeatured = cached.IsFeatured,
            HasFiles = hasFiles,
            IsNsfw = cached.IsNsfw,
            SourceSite = site,
            IsInstalled = isInstalled,
            IsEnabled = isInstalled,
            DeadlockerScraper = deadlockerScraper
        };

        // Теги
        vm.Tags.Add(cached.Category.ToString());
        if (!string.IsNullOrEmpty(cached.TargetHero))
        {
            vm.Tags.Add(cached.TargetHero);
        }
        vm.Tags.Add("Deadlock");
        vm.Tags.Add($"v{cached.Version}");

        // Что изменяет
        vm.WhatChanges.Add("Обновленные высокодетализированные текстуры и шейдеры");
        vm.WhatChanges.Add("Кастомные эффекты способностей и визуальный стиль");
        vm.WhatChanges.Add("Полная совместимость с серверами Valve Deadlock (без банов)");
        vm.WhatChanges.Add("Монтирование в Source 2 game/citadel/addons");

        if (!string.IsNullOrEmpty(cached.PreviewImageUrl))
        {
            vm.Screenshots.Add(cached.PreviewImageUrl);
        }

        vm.UpdateActionButtonText();
        vm.LoadPreview();
        return vm;
    }

    public static ModCardViewModel FromInstalledMod(
        InstalledMod installed,
        IDownloadService downloadService,
        IFileSystemService fileSystemService,
        ILocalModRepository modRepository,
        Action<ModCardViewModel>? onSelected = null)
    {
        var vm = new ModCardViewModel(downloadService, fileSystemService, modRepository, null, onSelected)
        {
            Id = installed.Id,
            Name = installed.Name,
            Description = installed.Description,
            Author = installed.Author,
            Version = installed.Version,
            Category = installed.Category,
            TargetHero = installed.TargetHero,
            DownloadCountFormatted = "—",
            Rating = 5.0,
            FileSizeBytes = installed.FileSizeBytes,
            FormattedSize = installed.FormattedSize,
            PreviewImageUrl = installed.LocalPreviewPath,
            DateFormatted = installed.InstallDate.ToString("dd.MM.yyyy"),
            IsInstalled = true,
            IsEnabled = installed.IsEnabled
        };
        vm.UpdateActionButtonText();
        vm.LoadPreview();
        return vm;
    }

    public void LoadPreview()
    {
        if (string.IsNullOrWhiteSpace(PreviewImageUrl) || PreviewBitmap != null) return;
        _ = Task.Run(async () =>
        {
            try
            {
                var bmp = await ImageCacheService.LoadBitmapAsync(PreviewImageUrl);
                if (bmp != null)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        PreviewBitmap = bmp;
                    });
                }
            }
            catch
            {
                // Игнорируем сетевые ошибки превью
            }
        });
    }

    [RelayCommand]
    public void Select()
    {
        _onSelected?.Invoke(this);
    }

    [RelayCommand]
    public void OpenDetail()
    {
        OnOpenDetail?.Invoke(this);
    }

    [RelayCommand]
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
    }

    [RelayCommand]
    public void OpenWebPage()
    {
        try
        {
            string? url = null;
            if (Id.StartsWith("gb_") && long.TryParse(Id[3..], out var gbId))
            {
                url = $"https://gamebanana.com/mods/{gbId}";
            }
            else if (Id.StartsWith("dl_"))
            {
                url = "https://deadlocker.net";
            }

            if (!string.IsNullOrEmpty(url))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Игнорируем ошибку запуска внешнего браузера
        }
    }

    [RelayCommand]
    public async Task InstallOrToggleAsync(CancellationToken cancellationToken = default)
    {
        if (IsInstalling) return;

        if (IsInstalled)
        {
            try
            {
                var newState = await _fileSystemService.ToggleModStateAsync(Id, cancellationToken);
                IsEnabled = newState;
                UpdateActionButtonText();
            }
            catch
            {
                ActionButtonText = Loc["Btn_Error"];
            }
            return;
        }

        // Если ссылка еще не была получена заранее, запрашиваем ее автоматически через GameBanana API
        if (string.IsNullOrWhiteSpace(DownloadUrl) && _gameBananaClient != null && (Id.StartsWith("gb_") || Id.StartsWith("gb-")))
        {
            ActionButtonText = Loc["Btn_Downloading"];
            if (long.TryParse(Id.Replace("gb_", "").Replace("gb-", ""), out var gbId))
            {
                DownloadUrl = await _gameBananaClient.GetDownloadUrlAsync(gbId, cancellationToken) ?? string.Empty;
            }
        }

        // Если ссылка Deadlocker еще не была получена, запрашиваем детали мода с Deadlocker API
        if (string.IsNullOrWhiteSpace(DownloadUrl) && DeadlockerScraper != null && Id.StartsWith("dl_"))
        {
            ActionButtonText = Loc["Btn_Downloading"];
            var rawId = Id.StartsWith("dl_") ? Id[3..] : Id;
            var details = await DeadlockerScraper.GetDeadlockerModDetailsAsync(rawId, cancellationToken);
            if (details != null && !string.IsNullOrWhiteSpace(details.DownloadUrl))
            {
                DownloadUrl = details.DownloadUrl;
            }
        }

        if (string.IsNullOrWhiteSpace(DownloadUrl))
        {
            ActionButtonText = Loc.IsEnglish ? "No file" : "Нет файла";
            return;
        }

        IsInstalling = true;
        InstallProgress = 0.0;
        ActionButtonText = Loc["Btn_Downloading"];

        try
        {
            var progress = new Progress<double>(p =>
            {
                InstallProgress = p;
                ActionButtonText = $"{Math.Round(p * 100)}%";
            });

            using var package = await _downloadService.DownloadAndUnpackAsync(DownloadUrl, Name, progress, cancellationToken);

            ActionButtonText = Loc["Btn_Installing"];

            var metadata = new ModMetadata
            {
                Name = Name,
                Description = Description,
                Version = Version,
                Author = Author,
                Category = Category,
                TargetHero = TargetHero,
                RemoteModId = Id
            };

            foreach (var vpkPath in package.VpkFilePaths)
            {
                await _fileSystemService.InstallModAsync(vpkPath, metadata, cancellationToken);
            }

            IsInstalled = true;
            IsEnabled = true;
            InstallProgress = 1.0;
            UpdateActionButtonText();
        }
        catch (OperationCanceledException)
        {
            ActionButtonText = Loc["Btn_Cancel"];
            InstallProgress = 0.0;
        }
        catch
        {
            ActionButtonText = Loc["Btn_Error"];
            InstallProgress = 0.0;
        }
        finally
        {
            IsInstalling = false;
        }
    }
}
