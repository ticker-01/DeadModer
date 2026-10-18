using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Entities;
using DeadModer.Core.Enums;
using DeadModer.Core.Interfaces.Data;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace DeadModer.Infrastructure.Data;

/// <summary>
/// Реализация репозитория локальных модов поверх LiteDB.
/// Все дисковые I/O операции вынесены в фоновые задачи (Task.Run) для предотвращения фризов интерфейса Avalonia.
/// </summary>
public sealed class LocalModRepository : ILocalModRepository
{
    private const string CollectionName = "installed_mods";
    private readonly ILiteDbContext _context;
    private readonly ILogger<LocalModRepository>? _logger;

    public LocalModRepository(ILiteDbContext context, ILogger<LocalModRepository>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    private ILiteCollection<InstalledMod> Collection => _context.GetCollection<InstalledMod>(CollectionName);

    /// <inheritdoc />
    public Task<IReadOnlyList<InstalledMod>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<InstalledMod>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var list = Collection
                .Query()
                .OrderByDescending(x => x.InstallDate)
                .ToList();

            _logger?.LogDebug("Загружено {Count} установленных модов из базы", list.Count);
            return list;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<InstalledMod?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return Task.Run<InstalledMod?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Collection.FindById(id);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<InstalledMod>> SearchAsync(string searchQuery, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<InstalledMod>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return Collection.Query().OrderByDescending(x => x.InstallDate).ToList();
            }

            var cleanQuery = searchQuery.Trim();
            // Регистронезависимый поиск по названию, автору и тегам
            var matches = Collection
                .Find(x => x.Name.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ||
                           x.Author.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ||
                           (x.TargetHero != null && x.TargetHero.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.InstallDate)
                .ToList();

            return matches;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<InstalledMod>> GetByCategoryAsync(ModCategory category, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<InstalledMod>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var list = Collection
                .Find(x => x.Category == category)
                .OrderByDescending(x => x.InstallDate)
                .ToList();

            return list;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<InstalledMod>> GetEnabledModsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<InstalledMod>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var list = Collection
                .Find(x => x.IsEnabled)
                .OrderByDescending(x => x.InstallDate)
                .ToList();

            return list;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<InstalledMod> AddAsync(InstalledMod mod, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mod);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(mod.Id))
            {
                mod.Id = Guid.NewGuid().ToString("N");
            }

            mod.InstallDate = DateTime.UtcNow;

            Collection.Insert(mod);
            _logger?.LogInformation("Мод '{Name}' ({Id}) успешно добавлен в LiteDB", mod.Name, mod.Id);
            return mod;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(InstalledMod mod, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mod);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            mod.LastUpdatedDate = DateTime.UtcNow;
            var success = Collection.Update(mod);

            if (success)
            {
                _logger?.LogDebug("Мод '{Name}' ({Id}) успешно обновлен", mod.Name, mod.Id);
            }
            else
            {
                _logger?.LogWarning("Не удалось обновить мод '{Name}' ({Id}) — запись не найдена", mod.Name, mod.Id);
            }

            return success;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var deleted = Collection.Delete(id);
            if (deleted)
            {
                _logger?.LogInformation("Мод с Id '{Id}' удален из LiteDB", id);
            }

            return deleted;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mod = Collection.FindById(id);
            if (mod == null)
            {
                _logger?.LogWarning("Мод с Id '{Id}' не найден для смены статуса IsEnabled", id);
                return false;
            }

            mod.IsEnabled = isEnabled;
            mod.LastUpdatedDate = DateTime.UtcNow;

            return Collection.Update(mod);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Collection.Exists(x => x.Id == id);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Collection.Count();
        }, cancellationToken);
    }
}
