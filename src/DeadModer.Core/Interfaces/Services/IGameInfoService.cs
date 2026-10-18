using System.Threading;
using System.Threading.Tasks;

namespace DeadModer.Core.Interfaces.Services;

/// <summary>
/// Сервис управления конфигурацией gameinfo.gi Source 2 для включения поддержки модов.
/// Реализует стандарты установки модификаций Deadlock (Deadlocker.net/install-guide).
/// </summary>
public interface IGameInfoService
{
    /// <summary>
    /// Проверяет, настроен ли gameinfo.gi для монтирования папки citadel/addons.
    /// </summary>
    Task<bool> IsModdingEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Вносит изменения в gameinfo.gi для автоматического монтирования citadel/addons,
    /// предварительно создавая резервную копию gameinfo.gi.bak.
    /// </summary>
    Task<bool> EnableModdingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Восстанавливает исходный файл gameinfo.gi из резервной копии gameinfo.gi.bak.
    /// </summary>
    Task<bool> RestoreOriginalGameInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает физический путь к файлу gameinfo.gi в установке Deadlock.
    /// </summary>
    Task<string?> GetGameInfoPathAsync(CancellationToken cancellationToken = default);
}
