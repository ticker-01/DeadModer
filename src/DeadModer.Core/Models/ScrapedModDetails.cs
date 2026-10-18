using System.Collections.Generic;

namespace DeadModer.Core.Models;

/// <summary>
/// Результат парсинга страницы модификации Deadlocker.net / GameBanana.
/// </summary>
public class ScrapedModDetails
{
    public string ModId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = "Deadlock Community";
    public string Version { get; set; } = "1.0.0";
    public string DownloadUrl { get; set; } = string.Empty;
    public string? PreviewImageUrl { get; set; }
    public string FileExtension { get; set; } = ".vpk";
    public string SourceUrl { get; set; } = string.Empty;
    public string? HeroName { get; set; }
    public string Category { get; set; } = "Other";
    public double Rating { get; set; } = 4.8;
    public int DownloadCount { get; set; } = 0;
    public long FileSizeBytes { get; set; } = 12 * 1024 * 1024;
    public List<string> Tags { get; set; } = [];
    public List<string> Screenshots { get; set; } = [];
}
