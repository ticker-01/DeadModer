using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Entities;
using DeadModer.Core.Enums;

namespace DeadModer.Core.Interfaces.Data;

/// <summary>
/// Репозиторий локального кэша каталога модов.
/// </summary>
public interface ICachedModRepository
{
    /// <summary>
    /// Получить все закэшированные моды.
    /// </summary>
    Task<IReadOnlyList<CachedModInfo>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить топ популярных модов (сортировка по количеству загрузок).
    /// </summary>
    Task<IReadOnlyList<CachedModInfo>> GetPopularAsync(int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить последние обновленные моды (сортировка по PublishedDate).
    /// </summary>
    Task<IReadOnlyList<CachedModInfo>> GetRecentAsync(int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить закэшированные моды определенной категории.
    /// </summary>
    Task<IReadOnlyList<CachedModInfo>> GetByCategoryAsync(ModCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить закэшированные моды определенного источника ("GameBanana", "Deadlocker").
    /// </summary>
    Task<IReadOnlyList<CachedModInfo>> GetBySiteAsync(string site, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти закэшированный мод по первичному ключу.
    /// </summary>
    Task<CachedModInfo?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить или обновить информацию об отдельном моде.
    /// </summary>
    Task UpsertAsync(CachedModInfo mod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Массово сохранить или обновить информацию о модах из удаленного каталога.
    /// </summary>
    Task UpsertRangeAsync(IEnumerable<CachedModInfo> mods, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить отдельную запись из кэша.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Очистить весь кэш каталога.
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
