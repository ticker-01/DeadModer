using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DeadModer.Core.Models.GameBanana;

/// <summary>
/// DTO корневого ответа эндпоинта Subfeed из GameBanana REST API (apiv11/Game/{id}/Subfeed).
/// </summary>
public class GameBananaSubfeedResponseDto
{
    [JsonPropertyName("_aMetadata")]
    public GameBananaSubfeedMetadataDto? Metadata { get; set; }

    [JsonPropertyName("_aRecords")]
    public List<GameBananaSubfeedRecordDto> Records { get; set; } = [];
}

public class GameBananaSubfeedMetadataDto
{
    [JsonPropertyName("_nRecordCount")]
    public int RecordCount { get; set; }

    [JsonPropertyName("_nPerpage")]
    public int PerPage { get; set; }

    [JsonPropertyName("_bIsComplete")]
    public bool IsComplete { get; set; }
}

public class GameBananaSubfeedRecordDto
{
    [JsonPropertyName("_idRow")]
    public long Id { get; set; }

    [JsonPropertyName("_sName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_sModelName")]
    public string ModelName { get; set; } = "Mod";

    [JsonPropertyName("_sProfileUrl")]
    public string ProfileUrl { get; set; } = string.Empty;

    [JsonPropertyName("_sVersion")]
    public string? Version { get; set; }

    [JsonPropertyName("_tsDateAdded")]
    public long TimestampAdded { get; set; }

    [JsonPropertyName("_tsDateModified")]
    public long? TimestampModified { get; set; }

    [JsonPropertyName("_nLikeCount")]
    public int LikeCount { get; set; }

    [JsonPropertyName("_nViewCount")]
    public int ViewCount { get; set; }

    [JsonPropertyName("_aSubmitter")]
    public GameBananaSubmitterDto? Submitter { get; set; }

    [JsonPropertyName("_aRootCategory")]
    public GameBananaCategoryDto? RootCategory { get; set; }

    [JsonPropertyName("_aSubCategory")]
    public GameBananaCategoryDto? SubCategory { get; set; }

    [JsonPropertyName("_aPreviewMedia")]
    public GameBananaPreviewMediaWrapperDto? PreviewMedia { get; set; }
}
