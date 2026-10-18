using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using DeadModer.Core.Interfaces.Services;
using DeadModer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeadModer.Infrastructure.Services;

/// <summary>
/// Реализация клиента/скрапера Deadlocker.net.
/// Поддерживает автоматический парсинг официального API (https://deadlocker.net/api/v1/mods)
/// и AngleSharp скрапинг страниц.
/// </summary>
public sealed class DeadlockerScraper : IDeadlockerScraper
{
    private const string ApiBaseUrl = "https://deadlocker.net/api/v1/";
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeadlockerScraper>? _logger;
    private readonly HtmlParser _htmlParser;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DeadlockerScraper(HttpClient httpClient, ILogger<DeadlockerScraper>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
        _htmlParser = new HtmlParser();

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScrapedModDetails>> FetchDeadlockerModsAsync(int page = 1, int limit = 40, CancellationToken cancellationToken = default)
    {
        var endpoint = $"https://deadlocker.net/api/v1/mods?page={page}&limit={limit}";
        _logger?.LogInformation("Автоматический запрос модов с Deadlocker API: {Endpoint}", endpoint);

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Deadlocker API вернул статус {StatusCode}", response.StatusCode);
                return [];
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = doc.RootElement;

            if (!root.TryGetProperty("mods", out var modsArray) || modsArray.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<ScrapedModDetails>();

            foreach (var el in modsArray.EnumerateArray())
            {
                var modId = el.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                var title = el.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Deadlock Mod" : "Deadlock Mod";
                var desc = el.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
                var mainImage = el.TryGetProperty("main_image", out var imgProp) ? imgProp.GetString() : null;
                var charName = el.TryGetProperty("character_name", out var charProp) ? charProp.GetString() : null;
                var category = el.TryGetProperty("gb_category", out var catProp) ? catProp.GetString() ?? "Skins" : "Skins";
                var downloads = el.TryGetProperty("downloads", out var dlProp) && dlProp.TryGetInt32(out var dlVal) ? dlVal : 0;
                var gbDownloads = el.TryGetProperty("gb_downloads", out var gbdlProp) && gbdlProp.TryGetInt32(out var gbdlVal) ? gbdlVal : 0;

                // Извлекаем автора из описания (например, "Yamato by Kirill Senzu")
                var author = "Deadlock Modder";
                var byIndex = desc.LastIndexOf(" by ", StringComparison.OrdinalIgnoreCase);
                if (byIndex >= 0 && byIndex + 4 < desc.Length)
                {
                    author = desc[(byIndex + 4)..].Trim();
                }

                var mod = new ScrapedModDetails
                {
                    ModId = modId,
                    Title = title,
                    Description = desc,
                    Author = author,
                    Version = "1.0.0",
                    PreviewImageUrl = mainImage,
                    HeroName = charName,
                    Category = category,
                    DownloadCount = downloads > 0 ? downloads : gbDownloads,
                    Rating = 4.8,
                    SourceUrl = $"https://deadlocker.net/mods/{modId}",
                    Tags = [category, charName ?? "Deadlock"]
                };

                result.Add(mod);
            }

            _logger?.LogInformation("Успешно распарсено {Count} модов с Deadlocker API", result.Count);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Ошибка при парсинге API Deadlocker.net");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<ScrapedModDetails?> GetDeadlockerModDetailsAsync(string modId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modId)) return null;

        var endpoint = $"https://deadlocker.net/api/v1/mods/{modId}";
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = doc.RootElement;

            if (!root.TryGetProperty("mod", out var modEl)) return null;

            var title = modEl.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
            var desc = modEl.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
            var downloadUrl = modEl.TryGetProperty("download_url", out var dlProp) ? dlProp.GetString() ?? "" : "";
            var mainImage = modEl.TryGetProperty("main_image", out var imgProp) ? imgProp.GetString() : null;
            var charName = modEl.TryGetProperty("character_name", out var charProp) ? charProp.GetString() : null;
            var rating = modEl.TryGetProperty("rating", out var rateProp) && rateProp.TryGetDouble(out var rVal) ? rVal : 4.8;
            var downloads = modEl.TryGetProperty("downloads", out var downProp) && downProp.TryGetInt32(out var dVal) ? dVal : 1200;

            var screenshots = new List<string>();
            if (!string.IsNullOrEmpty(mainImage)) screenshots.Add(mainImage);

            if (modEl.TryGetProperty("additional_images", out var addImgs) && addImgs.ValueKind == JsonValueKind.Array)
            {
                foreach (var img in addImgs.EnumerateArray())
                {
                    var url = img.GetString();
                    if (!string.IsNullOrEmpty(url)) screenshots.Add(url);
                }
            }

            return new ScrapedModDetails
            {
                ModId = modId,
                Title = title,
                Description = desc,
                DownloadUrl = downloadUrl,
                PreviewImageUrl = mainImage,
                HeroName = charName,
                Rating = rating,
                DownloadCount = downloads,
                Screenshots = screenshots
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Ошибка получения деталей мода {ModId} с Deadlocker", modId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ScrapedModDetails?> ScrapeModPageAsync(string pageUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageUrl);

        try
        {
            // Если передан ID или ссылка вида deadlocker.net/mods/{id}
            if (pageUrl.Contains("deadlocker.net/mods/"))
            {
                var modId = pageUrl.Split("/mods/").Last().Split('?').First().Trim('/');
                var apiResult = await GetDeadlockerModDetailsAsync(modId, cancellationToken);
                if (apiResult != null) return apiResult;
            }

            _logger?.LogDebug("Загрузка HTML страницы мода: {Url}", pageUrl);
            var response = await _httpClient.GetAsync(pageUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Не удалось загрузить страницу {Url}. HTTP статус: {StatusCode}", pageUrl, response.StatusCode);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var document = await _htmlParser.ParseDocumentAsync(html, cancellationToken);
            var baseUri = new Uri(pageUrl);

            var title = document.QuerySelector("meta[property='og:title']")?.GetAttribute("content")
                        ?? document.QuerySelector("h1.mod-title, h1.entry-title, h1")?.TextContent?.Trim()
                        ?? document.Title?.Trim()
                        ?? "Без названия";

            if (title.Contains('|')) title = title.Split('|')[0].Trim();

            var description = document.QuerySelector("meta[property='og:description']")?.GetAttribute("content")
                              ?? document.QuerySelector(".mod-description, .entry-content p, .description")?.TextContent?.Trim()
                              ?? string.Empty;

            var author = document.QuerySelector("meta[name='author']")?.GetAttribute("content")
                         ?? document.QuerySelector(".author, .mod-author, a[rel='author'], .creator")?.TextContent?.Trim()
                         ?? "Неизвестный автор";

            var downloadUrl = ExtractDownloadUrlFromDocument(document, baseUri) ?? string.Empty;

            var previewUrl = document.QuerySelector("meta[property='og:image']")?.GetAttribute("content")
                             ?? document.QuerySelector(".mod-preview img, .featured-image img, article img")?.GetAttribute("src");

            if (!string.IsNullOrEmpty(previewUrl) && Uri.TryCreate(baseUri, previewUrl, out var fullPreviewUri))
            {
                previewUrl = fullPreviewUri.ToString();
            }

            var version = ExtractVersion(document.Body?.TextContent ?? string.Empty);
            var ext = downloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? ".zip" : ".vpk";

            return new ScrapedModDetails
            {
                Title = title,
                Description = description,
                Author = author,
                Version = version,
                DownloadUrl = downloadUrl,
                PreviewImageUrl = previewUrl,
                FileExtension = ext,
                SourceUrl = pageUrl
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Ошибка парсинга страницы мода {Url}", pageUrl);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ExtractDirectDownloadUrlAsync(string pageUrl, CancellationToken cancellationToken = default)
    {
        var details = await ScrapeModPageAsync(pageUrl, cancellationToken);
        return details?.DownloadUrl;
    }

    private static string? ExtractDownloadUrlFromDocument(AngleSharp.Html.Dom.IHtmlDocument document, Uri baseUri)
    {
        var anchors = document.QuerySelectorAll("a[href]").ToList();

        foreach (var a in anchors)
        {
            var href = a.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href)) continue;

            if (href.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase) ||
                href.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                href.Contains("/download/", StringComparison.OrdinalIgnoreCase) ||
                href.Contains("gamebanana.com/dl/", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.TryCreate(baseUri, href, out var fullUri) ? fullUri.ToString() : href;
            }
        }

        return null;
    }

    private static string ExtractVersion(string text)
    {
        var match = Regex.Match(text, @"(?:version|v|версия)\s*:?\s*(\d+\.\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "1.0.0";
    }
}
