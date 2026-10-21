using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeadModer.Core.Entities;
using DeadModer.Core.Enums;
using DeadModer.Core.Interfaces.Data;
using DeadModer.Core.Interfaces.Services;
using DeadModer.UI.Services;
using Microsoft.Extensions.Logging;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// ViewModel для вкладки "Мои моды" (управление локально установленными .vpk файлами).
/// </summary>
public partial class InstalledModsViewModel : ViewModelBase
{
    private readonly ILocalModRepository _modRepository;
    private readonly IFileSystemService _fileSystemService;
    private readonly IDownloadService _downloadService;
    private readonly ISteamLocator _steamLocator;
    private readonly IModCompatibilityService? _modCompatibilityService;
    private readonly ILogger<InstalledModsViewModel>? _logger;
    private Action<ModCardViewModel>? _onSelected;

    public ObservableCollection<ModCardViewModel> InstalledMods { get; } = [];

    [ObservableProperty]
    private int _totalInstalledCount = 0;

    [ObservableProperty]
    private int _enabledCount = 0;

    [ObservableProperty]
    private string _totalStorageUsedFormatted = "0 MB";

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isFixingMods = false;

    [ObservableProperty]
    private string? _fixStatusMessage;

    [ObservableProperty]
    private ModCardViewModel? _selectedMod;

    public InstalledModsViewModel()
    {
        _modRepository = null!;
        _fileSystemService = null!;
        _downloadService = null!;
        _steamLocator = null!;
        _modCompatibilityService = null;
        _logger = null;

        var sample1 = new ModCardViewModel { Name = "Better HUD", Author = "Deadmoder", Description = "Clean HUD layout", FormattedSize = "12.4 MB", IsInstalled = true, IsEnabled = true };
        sample1.UpdateActionButtonText();
        var sample2 = new ModCardViewModel { Name = "No Minimap Fog", Author = "Deadmoder", Description = "Clear minimap vision", FormattedSize = "8.7 MB", IsInstalled = true, IsEnabled = true };
        sample2.UpdateActionButtonText();
        InstalledMods.Add(sample1);
        InstalledMods.Add(sample2);
        SelectedMod = sample1;
        TotalInstalledCount = 2;
        EnabledCount = 2;
        TotalStorageUsedFormatted = "21.1 MB";
    }

    public InstalledModsViewModel(
        ILocalModRepository modRepository,
        IFileSystemService fileSystemService,
        IDownloadService downloadService,
        ISteamLocator steamLocator,
        IModCompatibilityService? modCompatibilityService = null,
        Action<ModCardViewModel>? onSelected = null,
        ILogger<InstalledModsViewModel>? logger = null)
    {
        _modRepository = modRepository ?? throw new ArgumentNullException(nameof(modRepository));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
        _steamLocator = steamLocator ?? throw new ArgumentNullException(nameof(steamLocator));
        _modCompatibilityService = modCompatibilityService;
        _onSelected = onSelected;
        _logger = logger;
    }

    public void SetSelectionCallback(Action<ModCardViewModel> onSelected)
    {
        _onSelected = onSelected;
    }

    [RelayCommand]
    public void SelectMod(ModCardViewModel? card)
    {
        if (card == null) return;
        SelectedMod = card;
        _onSelected?.Invoke(card);
    }

    [RelayCommand]
    public async Task LoadInstalledModsAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var mods = await _modRepository.GetAllAsync(cancellationToken);
            InstalledMods.Clear();

            long totalBytes = 0;
            int enabled = 0;

            foreach (var mod in mods)
            {
                var card = ModCardViewModel.FromInstalledMod(
                    mod, 
                    _downloadService, 
                    _fileSystemService, 
                    _modRepository, 
                    c => SelectMod(c));

                totalBytes += mod.FileSizeBytes;
                if (mod.IsEnabled) enabled++;

                InstalledMods.Add(card);
            }

            TotalInstalledCount = InstalledMods.Count;
            EnabledCount = enabled;
            TotalStorageUsedFormatted = FormatSize(totalBytes);

            if (SelectedMod == null && InstalledMods.Count > 0)
            {
                SelectMod(InstalledMods[0]);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при загрузке списка установленных модов");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SyncAddonsFolderAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        try
        {
            var addonsPath = await _steamLocator.GetAddonsPathAsync(cancellationToken);
            if (!Directory.Exists(addonsPath)) return;

            // Автоматически нормализуем любые нестандартные файлы в pakXX_dir.vpk по стандарту Source 2
            await _fileSystemService.MigrateNonPakFilesAsync(cancellationToken);

            var existingMods = await _modRepository.GetAllAsync(cancellationToken);
            var knownFiles = new HashSet<string>(
                existingMods.SelectMany(m => m.VpkFiles), 
                StringComparer.OrdinalIgnoreCase);

            var dirFiles = Directory.GetFiles(addonsPath, "*.*")
                .Where(f => f.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".vpk.disabled", StringComparison.OrdinalIgnoreCase));

            foreach (var file in dirFiles)
            {
                var fileName = Path.GetFileName(file);
                if (!knownFiles.Contains(fileName))
                {
                    var isEnabled = fileName.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase);
                    var fileInfo = new FileInfo(file);
                    var cleanName = Path.GetFileNameWithoutExtension(fileName.Replace(".disabled", ""));

                    var newMod = new InstalledMod
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = cleanName,
                        Description = Loc.IsEnglish ? "Discovered in Deadlock addons directory" : "Обнаружен в папке addons игры Deadlock",
                        Author = Loc["Local_Detected"],
                        Version = "1.0",
                        Category = ModCategory.Other,
                        IsEnabled = isEnabled,
                        InstallDate = fileInfo.CreationTimeUtc,
                        FileSizeBytes = fileInfo.Length,
                        ModDirectoryPath = addonsPath,
                        VpkFiles = [fileName],
                        Tags = ["Local", "Addons"]
                    };

                    await _modRepository.AddAsync(newMod, cancellationToken);
                    _logger?.LogInformation("Синхронизирован внешний мод: {FileName}", fileName);
                }
            }

            await LoadInstalledModsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при синхронизации с папкой addons");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ToggleModAsync(ModCardViewModel? card, CancellationToken cancellationToken = default)
    {
        if (card == null) return;
        try
        {
            var newState = await _fileSystemService.ToggleModStateAsync(card.Id, cancellationToken);
            card.IsEnabled = newState;
            card.UpdateActionButtonText();
            EnabledCount = InstalledMods.Count(m => m.IsEnabled);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка переключения мода {ModId}", card.Id);
        }
    }

    [RelayCommand]
    public async Task DeleteModAsync(ModCardViewModel? card, CancellationToken cancellationToken = default)
    {
        if (card == null) return;
        try
        {
            var success = await _fileSystemService.UninstallModAsync(card.Id, cancellationToken);
            if (success)
            {
                InstalledMods.Remove(card);
                TotalInstalledCount = InstalledMods.Count;
                EnabledCount = InstalledMods.Count(m => m.IsEnabled);
                if (SelectedMod?.Id == card.Id)
                {
                    SelectedMod = InstalledMods.FirstOrDefault();
                    if (SelectedMod != null) _onSelected?.Invoke(SelectedMod);
                }
                _logger?.LogInformation("Мод {Name} успешно удален", card.Name);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка удаления мода {ModId}", card.Id);
        }
    }

    [RelayCommand]
    public async Task ToggleAllModsAsync(bool enable, CancellationToken cancellationToken = default)
    {
        foreach (var card in InstalledMods.ToList())
        {
            if (card.IsEnabled != enable)
            {
                await _fileSystemService.ToggleModStateAsync(card.Id, cancellationToken);
                card.IsEnabled = enable;
                card.UpdateActionButtonText();
            }
        }
        EnabledCount = enable ? InstalledMods.Count : 0;
    }

    [RelayCommand]
    public async Task OpenAddonsFolderAsync()
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
            _logger?.LogError(ex, "Ошибка при открытии папки addons");
        }
    }

    [RelayCommand]
    public async Task FixAllModsAsync(CancellationToken cancellationToken = default)
    {
        if (IsFixingMods || _modCompatibilityService == null) return;

        IsFixingMods = true;
        FixStatusMessage = Loc["FixMods_Running"];

        try
        {
            // 1. Автоматически нормализуем нестандартные имена VPK (pakXX_dir.vpk)
            await _fileSystemService.MigrateNonPakFilesAsync(cancellationToken);

            var progress = new Progress<string>(msg =>
            {
                FixStatusMessage = msg;
            });

            var reports = await _modCompatibilityService.RunUpdaterAsync(
                customDeadlockDir: null,
                maxWorkers: 8,
                progress: progress,
                cancellationToken: cancellationToken);

            if (reports.Count == 0)
            {
                FixStatusMessage = Loc["FixMods_NoMods"];
            }
            else
            {
                int updated = reports.Count(r => r.WasUpdated);
                int ok = reports.Count(r => r.Status.StartsWith("OK", StringComparison.OrdinalIgnoreCase));
                int errors = reports.Count(r => !r.IsSuccess);
                FixStatusMessage = Loc.IsEnglish
                    ? $"Mod update complete. Updated: {updated}, Up to date: {ok}, Errors: {errors}."
                    : $"Проверка завершена. Обновлено: {updated}, Без изменений: {ok}, Ошибок: {errors}.";
            }

            await LoadInstalledModsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при восстановлении совместимости модов");
            FixStatusMessage = Loc.IsEnglish ? $"Error: {ex.Message}" : $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsFixingMods = false;
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
