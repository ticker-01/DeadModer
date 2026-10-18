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
/// Репозиторий локального кэша каталога онлайн-модов в LiteDB.
/// </summary>
public sealed class CachedModRepository : ICachedModRepository
{
    private const string CollectionName = "cached_mods";
    private readonly ILiteDbContext _context;
    private readonly ILogger<CachedModRepository>? _logger;

    public CachedModRepository(ILiteDbContext context, ILogger<CachedModRepository>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    private ILiteCollection<CachedModInfo> Collection => _context.GetCollection<CachedModInfo>(CollectionName);

    /// <inheritdoc />
    public Task<IReadOnlyList<CachedModInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<CachedModInfo>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Collection.FindAll().ToList();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CachedModInfo>> GetPopularAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<CachedModInfo>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Collection
                .Query()
                .OrderByDescending(x => x.DownloadCount)
                .Limit(limit)
                .ToList();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CachedModInfo>> GetRecentAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<CachedModInfo>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Collection
                .Query()
                .OrderByDescending(x => x.PublishedDate)
                .Limit(limit)
                .ToList();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<CachedModInfo>> GetByCategoryAsync(ModCategory category, CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<CachedModInfo>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Collection
                .Find(x => x.Category == category)
                .OrderByDescending(x => x.DownloadCount)
                .ToList();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<CachedModInfo?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return Task.Run<CachedModInfo?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Collection.FindById(id);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpsertAsync(CachedModInfo mod, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mod);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Collection.Upsert(mod);
            _logger?.LogDebug("Обновлен кэш мода {ModId} в LiteDB", mod.Id);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpsertRangeAsync(IEnumerable<CachedModInfo> mods, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mods);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var list = mods.ToList();
            if (list.Count == 0) return;

            Collection.Upsert(list);
            _logger?.LogDebug("Обновлен кэш {Count} модов в LiteDB", list.Count);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Collection.Delete(id);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Collection.DeleteAll();
            _logger?.LogInformation("Кэш каталога модов полностью очищен");
        }, cancellationToken);
    }
}
