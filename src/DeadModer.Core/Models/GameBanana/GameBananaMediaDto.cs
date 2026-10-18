using System.Text.Json.Serialization;

namespace DeadModer.Core.Models.GameBanana;

/// <summary>
/// DTO медиа-элемента (скриншота/обложки) в GameBanana API.
/// </summary>
public class GameBananaMediaDto
{
    [JsonPropertyName("_sType")]
    public string Type { get; set; } = "image";

    [JsonPropertyName("_sBaseUrl")]
    public string BaseUrl { get; set; } = string.Empty;

    [JsonPropertyName("_sFile")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("_sFile220")]
    public string? File220 { get; set; }

    [JsonPropertyName("_sFile530")]
    public string? File530 { get; set; }

    /// <summary>
    /// Полный URL оригинального изображения.
    /// </summary>
    public string FullUrl =>
        !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(File)
            ? $"{BaseUrl.TrimEnd('/')}/{File.TrimStart('/')}"
            : string.Empty;

    /// <summary>
    /// URL оптимизированной миниатюры для карточки в каталоге.
    /// </summary>
    public string ThumbnailUrl
    {
        get
        {
            var target = !string.IsNullOrEmpty(File220) ? File220 : (!string.IsNullOrEmpty(File530) ? File530 : File);
            return !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(target)
                ? $"{BaseUrl.TrimEnd('/')}/{target.TrimStart('/')}"
                : string.Empty;
        }
    }
}
