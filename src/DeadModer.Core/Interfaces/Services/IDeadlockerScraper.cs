using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Models;

namespace DeadModer.Core.Interfaces.Services;

/// <summary>
/// Сервис интеграции с каталогом Deadlocker.net (REST API + AngleSharp парсинг).
/// Автоматически извлекает моды, метаданные, превью и прямые ссылки на скачивание.
/// </summary>
public interface IDeadlockerScraper
{
    /// <summary>
    /// Автоматически получает список модов через официальный Deadlocker.net API.
    /// </summary>
    Task<IReadOnlyList<ScrapedModDetails>> FetchDeadlockerModsAsync(int page = 1, int limit = 40, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает полные детальные данные мода Deadlocker (прямая ссылка, скриншоты, отзывы) по ID.
    /// </summary>
    Task<ScrapedModDetails?> GetDeadlockerModDetailsAsync(string modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Парсит HTML-страницу модификации и извлекает метаданные (название, автор, версия, прямая ссылка).
    /// </summary>
    Task<ScrapedModDetails?> ScrapeModPageAsync(string pageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Быстро извлекает прямую ссылку на скачивание (.vpk / .zip) со страницы мода.
    /// </summary>
    Task<string?> ExtractDirectDownloadUrlAsync(string pageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Автоматически получает полный актуальный список персонажей/героев Deadlock через официальный API.
    /// </summary>
    Task<IReadOnlyList<DeadlockCharacterDto>> FetchCharactersAsync(CancellationToken cancellationToken = default);
}
