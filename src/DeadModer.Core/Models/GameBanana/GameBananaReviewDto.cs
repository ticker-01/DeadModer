using System;

namespace DeadModer.Core.Models.GameBanana;

/// <summary>
/// DTO комментария или отзыва игрока с GameBanana REST API (apiv11).
/// </summary>
public class GameBananaReviewDto
{
    public long Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorAvatarUrl { get; set; } = string.Empty;
    public string AuthorTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public double Rating { get; set; } = 5.0;
}
