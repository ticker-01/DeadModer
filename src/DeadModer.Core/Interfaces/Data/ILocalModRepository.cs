using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Entities;
using DeadModer.Core.Enums;

namespace DeadModer.Core.Interfaces.Data;

/// <summary>
/// Репозиторий для управления локально установленными модами в LiteDB.
/// Предоставляет полный набор CRUD-операций и выборки с поддержкой асинхронности.
/// </summary>
public interface ILocalModRepository
{
    /// <summary>
    /// Получить все установленные моды.
    /// </summary>
    Task<IReadOnlyList<InstalledMod>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить установленный мод по его уникальному идентификатору.
    /// </summary>
    Task<InstalledMod?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти моды по названию, тегам или автору.
    /// </summary>
    Task<IReadOnlyList<InstalledMod>> SearchAsync(string searchQuery, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить установленные моды определенной категории.
    /// </summary>
    Task<IReadOnlyList<InstalledMod>> GetByCategoryAsync(ModCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить только активные (включенные) моды для развертывания в игру.
    /// </summary>
    Task<IReadOnlyList<InstalledMod>> GetEnabledModsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить новый установленный мод в базу данных.
    /// </summary>
    Task<InstalledMod> AddAsync(InstalledMod mod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить метаданные или состояние существующего мода.
    /// </summary>
    Task<bool> UpdateAsync(InstalledMod mod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить запись о моде из локальной базы данных.
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Быстро переключить состояние активности мода (включен/отключен).
    /// </summary>
    Task<bool> SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить наличие мода с указанным идентификатором в базе данных.
    /// </summary>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить общее количество установленных модов.
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
