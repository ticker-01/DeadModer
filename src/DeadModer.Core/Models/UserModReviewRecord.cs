using System;

namespace DeadModer.Core.Models;

/// <summary>
/// Запись пользовательского отзыва или комментария к моду, оставленного из приложения.
/// Сохраняется локально и отображается во вкладке отзывов модификации.
/// </summary>
public sealed class UserModReviewRecord
{
    public string ModId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorTitle { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Rating { get; set; } = 5.0;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}
