using System;
using System.Text.Json.Serialization;

namespace DeadModer.Core.Models.GameBanana;

/// <summary>
/// DTO файла модификации в REST API GameBanana.
/// </summary>
public class GameBananaFileDto
{
    [JsonPropertyName("_idRow")]
    public long Id { get; set; }

    [JsonPropertyName("_sFile")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("_nFilesize")]
    public long FileSizeBytes { get; set; }

    [JsonPropertyName("_sDownloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("_sDescription")]
    public string? Description { get; set; }

    [JsonPropertyName("_tsDateAdded")]
    public long TimestampAdded { get; set; }

    [JsonPropertyName("_sMd5Checksum")]
    public string? Md5Checksum { get; set; }

    public DateTime DateAdded => DateTimeOffset.FromUnixTimeSeconds(TimestampAdded).UtcDateTime;

    public bool IsVpkOrZip =>
        FileName.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase) ||
        FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
        FileName.EndsWith(".7z", StringComparison.OrdinalIgnoreCase) ||
        FileName.EndsWith(".rar", StringComparison.OrdinalIgnoreCase);
}
