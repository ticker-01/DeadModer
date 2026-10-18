using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DeadModer.Core.Interfaces.Services;

/// <summary>
/// Сервис для обнаружения Steam-библиотек и директорий установки Deadlock (AppID: 1422450).
/// </summary>
public interface ISteamLocator
{
    /// <summary>
    /// Steam AppID игры Deadlock.
    /// </summary>
    const int DeadlockAppId = 1422450;

    /// <summary>
    /// Найти путь к корневой директории клиента Steam на ПК.
    /// </summary>
    Task<string?> FindSteamInstallPathAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все пути к библиотекам Steam из libraryfolders.vdf.
    /// </summary>
    Task<IReadOnlyList<string>> GetAllSteamLibraryFoldersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Найти путь к установленной игре Deadlock (.../steamapps/common/Deadlock).
    /// </summary>
    Task<string?> FindDeadlockGamePathAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить абсолютный путь к папке аддонов/модов Source 2 (.../game/citadel/addons).
    /// Гарантирует создание директории, если она отсутствует.
    /// </summary>
    /// <exception cref="DeadModer.Core.Exceptions.GameNotFoundException">Если игра не найдена</exception>
    Task<string> GetAddonsPathAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить, установлена ли игра Deadlock в одной из библиотек Steam.
    /// </summary>
    Task<bool> IsGameInstalledAsync(CancellationToken cancellationToken = default);
}
