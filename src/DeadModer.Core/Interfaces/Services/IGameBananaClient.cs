using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Models.GameBanana;

namespace DeadModer.Core.Interfaces.Services;

/// <summary>
/// Клиент официального REST API GameBanana для получения каталога и ссылок на моды Deadlock.
/// </summary>
public interface IGameBananaClient
{
    /// <summary>
    /// ID игры Deadlock на платформе GameBanana (20948).
    /// </summary>
    const int DeadlockGameId = 20948;

    /// <summary>
    /// Получить подробную информацию о моде по его ID (включая список файлов для скачивания).
    /// </summary>
    Task<GameBananaModDto?> GetModDetailsAsync(long modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить список модов Deadlock из каталога GameBanana с пагинацией и сортировкой.
    /// </summary>
    Task<IReadOnlyList<GameBananaModDto>> GetDeadlockModsAsync(int page = 1, int perPage = 20, string sort = "default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить прямую ссылку на скачивание актуального .vpk или .zip файла мода.
    /// </summary>
    Task<string?> GetDownloadUrlAsync(long modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить список отзывов и комментариев игроков к моду по его ID с GameBanana REST API.
    /// </summary>
    Task<IReadOnlyList<GameBananaReviewDto>> GetModReviewsAsync(long modId, CancellationToken cancellationToken = default);
}
