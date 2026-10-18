using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeadModer.Core.Models.GameBanana;

/// <summary>
/// DTO полной информации о моде из GameBanana ProfilePage API (apiv11/Mod/{id}/ProfilePage).
/// </summary>
public class GameBananaModDto
{
    [JsonPropertyName("_idRow")]
    public long Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_sDescription")]
    public string ShortDescription { get; set; } = string.Empty;

    [JsonPropertyName("_sText")]
    public string BodyHtml { get; set; } = string.Empty;

    [JsonPropertyName("_tsDateAdded")]
    public long TimestampAdded { get; set; }

    [JsonPropertyName("_tsDateModified")]
    public long? TimestampModified { get; set; }

    [JsonPropertyName("_nViewCount")]
    public int ViewCount { get; set; }

    [JsonPropertyName("_nLikeCount")]
    public int LikeCount { get; set; }

    [JsonPropertyName("_sVersion")]
    public string? Version { get; set; }

    [JsonPropertyName("_nDownloadCount")]
    public int DownloadCount { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public GameBananaSubmitterDto? Submitter { get; set; }

    [JsonPropertyName("_aCategory")]
    public GameBananaCategoryDto? Category { get; set; }

    [JsonPropertyName("_aRootCategory")]
    public GameBananaCategoryDto? RootCategory { get; set; }

    [JsonPropertyName("_aSubCategory")]
    public GameBananaCategoryDto? SubCategory { get; set; }

    [JsonPropertyName("_bWasFeatured")]
    public bool WasFeatured { get; set; }

    [JsonPropertyName("_bHasFiles")]
    public bool HasFiles { get; set; } = true;

    [JsonPropertyName("_bHasContentRatings")]
    public bool HasContentRatings { get; set; }

    [JsonPropertyName("_aTags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("_aFiles")]
    public List<GameBananaFileDto> Files { get; set; } = [];

    [JsonPropertyName("_aPreviewMedia")]
    public GameBananaPreviewMediaWrapperDto? PreviewMediaWrapper { get; set; }

    public string AuthorName => Submitter?.Name ?? "Неизвестный автор";

    public DateTime DateAdded => DateTimeOffset.FromUnixTimeSeconds(TimestampAdded).UtcDateTime;

    public DateTime? DateModified => TimestampModified.HasValue && TimestampModified.Value > 0
        ? DateTimeOffset.FromUnixTimeSeconds(TimestampModified.Value).UtcDateTime
        : null;

    /// <summary>
    /// Ссылка на основной/первый VPK или ZIP файл для скачивания.
    /// </summary>
    public GameBananaFileDto? PrimaryFile =>
        Files.Find(f => f.FileName.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase))
        ?? Files.Find(f => f.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        ?? Files.Find(f => f.FileName.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
        ?? Files.Find(f => f.FileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
        ?? (Files.Count > 0 ? Files[0] : null);

    /// <summary>
    /// Обложка мода (оригинал).
    /// </summary>
    public string? PrimaryImageUrl =>
        PreviewMediaWrapper?.Images != null && PreviewMediaWrapper.Images.Count > 0
            ? PreviewMediaWrapper.Images[0].FullUrl
            : null;

    /// <summary>
    /// Оптимизированная миниатюра для карточки каталога (220px).
    /// </summary>
    public string? PrimaryThumbnailUrl =>
        PreviewMediaWrapper?.Images != null && PreviewMediaWrapper.Images.Count > 0
            ? PreviewMediaWrapper.Images[0].ThumbnailUrl
            : null;
}

public class GameBananaSubmitterDto
{
    [JsonPropertyName("_idRow")]
    public long Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_sAvatarUrl")]
    public string? AvatarUrl { get; set; }
}

public class GameBananaCategoryDto
{
    [JsonPropertyName("_idRow")]
    public long Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = string.Empty;
}

public class GameBananaPreviewMediaWrapperDto
{
    [JsonPropertyName("_aImages")]
    public List<GameBananaMediaDto> Images { get; set; } = [];
}
