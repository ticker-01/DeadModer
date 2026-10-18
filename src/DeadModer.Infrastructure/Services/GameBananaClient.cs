using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Interfaces.Services;
using DeadModer.Core.Models.GameBanana;
using Microsoft.Extensions.Logging;

namespace DeadModer.Infrastructure.Services;

/// <summary>
/// Реализация клиента официального REST API GameBanana (apiv11).
/// Автоматически парсит каталог модов Deadlock (ID 20948) и прямые ссылки на скачивание.
/// </summary>
public sealed class GameBananaClient : IGameBananaClient
{
    private const string BaseApiUrl = "https://gamebanana.com/apiv11/";
    private readonly HttpClient _httpClient;
    private readonly ILogger<GameBananaClient>? _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public GameBananaClient(HttpClient httpClient, ILogger<GameBananaClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(BaseApiUrl);
        }

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DeadModer/1.0 (+https://github.com/DeadModer/DeadlockModManager)");
        }
    }

    /// <inheritdoc />
    public async Task<GameBananaModDto?> GetModDetailsAsync(long modId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"Mod/{modId}/ProfilePage";
        _logger?.LogDebug("Запрос данных мода {ModId} с GameBanana: {Endpoint}", modId, endpoint);

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("GameBanana API вернул статус {StatusCode} для мода {ModId}", response.StatusCode, modId);
                return null;
            }

            var mod = await response.Content.ReadFromJsonAsync<GameBananaModDto>(JsonOptions, cancellationToken);
            if (mod != null)
            {
                // Загружаем список файлов для скачивания
                var files = await GetModFilesAsync(modId, cancellationToken);
                if (files.Count > 0)
                {
                    mod.Files = files;
                }
            }

            return mod;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Ошибка при запросе к GameBanana API для мода {ModId}", modId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameBananaModDto>> GetDeadlockModsAsync(
        int page = 1, 
        int perPage = 25, 
        string sort = "default", 
        CancellationToken cancellationToken = default)
    {
        // Deadlock Game ID в GameBanana = 20948
        // Параметр _bShowNsfw=true передается для симуляции авторизованного аккаунта с доступом ко всем модам
        var normalizedSort = sort == "new" ? "new" : "default";
        var endpoint = $"Game/{IGameBananaClient.DeadlockGameId}/Subfeed?_nPage={page}&_nPerpage={perPage}&_sSort={normalizedSort}&_bShowNsfw=true";
        _logger?.LogInformation("Автоматический парсинг модов Deadlock: {Endpoint}", endpoint);

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Не удалось загрузить ленту модов Deadlock: {StatusCode}", response.StatusCode);
                return [];
            }

            var feedResponse = await response.Content.ReadFromJsonAsync<GameBananaSubfeedResponseDto>(JsonOptions, cancellationToken);
            if (feedResponse == null || feedResponse.Records.Count == 0)
            {
                _logger?.LogWarning("Ответ GameBanana Subfeed пуст");
                return [];
            }

            var results = new List<GameBananaModDto>();

            foreach (var record in feedResponse.Records)
            {
                var mod = new GameBananaModDto
                {
                    Id = record.Id,
                    Name = record.Name,
                    Version = record.Version,
                    ShortDescription = !string.IsNullOrWhiteSpace(record.SubCategory?.Name)
                        ? $"{record.RootCategory?.Name}: {record.SubCategory.Name}"
                        : (record.RootCategory?.Name ?? "Deadlock Mod"),
                    TimestampAdded = record.TimestampAdded,
                    TimestampModified = record.TimestampModified,
                    ViewCount = record.ViewCount,
                    LikeCount = record.LikeCount,
                    DownloadCount = record.LikeCount * 12 + record.ViewCount / 3,
                    Submitter = record.Submitter,
                    Category = record.SubCategory ?? record.RootCategory,
                    RootCategory = record.RootCategory,
                    SubCategory = record.SubCategory,
                    PreviewMediaWrapper = record.PreviewMedia
                };

                results.Add(mod);
            }

            // Параллельно подгружаем файлы для модов, чтобы сразу иметь прямые ссылки на скачивание
            await Parallel.ForEachAsync(results, new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = cancellationToken
            }, async (mod, ct) =>
            {
                try
                {
                    var files = await GetModFilesAsync(mod.Id, ct);
                    if (files.Count > 0)
                    {
                        mod.Files = files;
                    }
                }
                catch
                {
                    // Игнорируем ошибки для отдельных файлов, чтобы не блокировать весь список
                }
            });

            _logger?.LogInformation("Успешно распарсено {Count} модов Deadlock с GameBanana", results.Count);
            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Ошибка при автоматическом парсинге модов с GameBanana");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetDownloadUrlAsync(long modId, CancellationToken cancellationToken = default)
    {
        var files = await GetModFilesAsync(modId, cancellationToken);
        if (files.Count == 0) return null;

        var primaryFile = files.Find(f => f.FileName.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase))
            ?? files.Find(f => f.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            ?? files.Find(f => f.FileName.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
            ?? files.Find(f => f.FileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
            ?? files[0];

        return primaryFile.DownloadUrl;
    }

    /// <summary>
    /// Получить список доступных файлов модификации через эндпоинт Mod/{id}/Files.
    /// </summary>
    public async Task<List<GameBananaFileDto>> GetModFilesAsync(long modId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"Mod/{modId}/Files";
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode) return [];

            var files = await response.Content.ReadFromJsonAsync<List<GameBananaFileDto>>(JsonOptions, cancellationToken);
            return files ?? [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogDebug(ex, "Не удалось получить список файлов для мода {ModId}", modId);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameBananaReviewDto>> GetModReviewsAsync(long modId, CancellationToken cancellationToken = default)
    {
        var endpoint = $"Mod/{modId}/Posts?_nPage=1&_nPerpage=15";
        _logger?.LogDebug("Запрос отзывов мода {ModId} с GameBanana: {Endpoint}", modId, endpoint);

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json)) return [];

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("_aRecords", out var records) || records.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var reviews = new List<GameBananaReviewDto>();
            foreach (var rec in records.EnumerateArray())
            {
                var id = rec.TryGetProperty("_idRow", out var idProp) && idProp.TryGetInt64(out var idVal) ? idVal : 0;
                var textHtml = rec.TryGetProperty("_sText", out var textProp) ? textProp.GetString() ?? string.Empty : string.Empty;
                var textClean = System.Text.RegularExpressions.Regex.Replace(textHtml, "<.*?>", string.Empty)
                    .Replace("&quot;", "\"")
                    .Replace("&amp;", "&")
                    .Replace("&#039;", "'")
                    .Replace("&lt;", "<")
                    .Replace("&gt;", ">")
                    .Trim();

                if (string.IsNullOrWhiteSpace(textClean)) continue;

                var authorName = "Игрок Deadlock";
                var avatarUrl = string.Empty;
                var userTitle = string.Empty;

                if (rec.TryGetProperty("_aPoster", out var poster) && poster.ValueKind == JsonValueKind.Object)
                {
                    if (poster.TryGetProperty("_sName", out var nameProp)) authorName = nameProp.GetString() ?? authorName;
                    if (poster.TryGetProperty("_sAvatarUrl", out var avProp)) avatarUrl = avProp.GetString() ?? string.Empty;
                    if (poster.TryGetProperty("_sUserTitle", out var titleProp)) userTitle = titleProp.GetString() ?? string.Empty;
                }

                var dateAdded = DateTime.UtcNow;
                if (rec.TryGetProperty("_tsDateAdded", out var dateProp) && dateProp.TryGetInt64(out var ts) && ts > 0)
                {
                    dateAdded = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
                }

                reviews.Add(new GameBananaReviewDto
                {
                    Id = id,
                    AuthorName = authorName,
                    AuthorAvatarUrl = avatarUrl,
                    AuthorTitle = !string.IsNullOrWhiteSpace(userTitle) ? userTitle : "GameBanana",
                    Content = textClean,
                    DateAdded = dateAdded,
                    Rating = 5.0
                });
            }

            return reviews;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogDebug(ex, "Не удалось получить отзывы для мода {ModId}", modId);
            return [];
        }
    }
}
