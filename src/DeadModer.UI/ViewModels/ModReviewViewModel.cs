using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DeadModer.Core.Models.GameBanana;
using DeadModer.UI.Services;

namespace DeadModer.UI.ViewModels;

/// <summary>
/// ViewModel отзыва / комментария игрока к моду с сайта GameBanana или Deadlocker.
/// </summary>
public partial class ModReviewViewModel : ViewModelBase
{
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorTitle { get; init; } = string.Empty;
    public string AuthorAvatarUrl { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string DateFormatted { get; init; } = string.Empty;
    public double Rating { get; init; } = 5.0;

    public string RatingFormatted => Rating.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

    [ObservableProperty]
    private Bitmap? _avatarBitmap;

    public ModReviewViewModel()
    {
    }

    public ModReviewViewModel(GameBananaReviewDto dto)
    {
        AuthorName = !string.IsNullOrWhiteSpace(dto.AuthorName) ? dto.AuthorName : "Игрок Deadlock";
        AuthorTitle = !string.IsNullOrWhiteSpace(dto.AuthorTitle) ? dto.AuthorTitle : "Сообщество GameBanana";
        AuthorAvatarUrl = dto.AuthorAvatarUrl ?? string.Empty;
        Content = dto.Content;
        Rating = dto.Rating;

        var elapsed = DateTime.UtcNow - dto.DateAdded;
        if (elapsed.TotalDays > 30)
            DateFormatted = dto.DateAdded.ToString("dd.MM.yyyy");
        else if (elapsed.TotalDays >= 1)
            DateFormatted = $"{(int)elapsed.TotalDays} дн. назад";
        else if (elapsed.TotalHours >= 1)
            DateFormatted = $"{(int)elapsed.TotalHours} ч. назад";
        else
            DateFormatted = "Только что";
    }

    public async Task LoadAvatarAsync()
    {
        if (string.IsNullOrWhiteSpace(AuthorAvatarUrl)) return;

        try
        {
            var bitmap = await ImageCacheService.LoadBitmapAsync(AuthorAvatarUrl);
            if (bitmap != null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    AvatarBitmap = bitmap;
                });
            }
        }
        catch
        {
            // Игнорируем сетевые ошибки отдельных аватарок
        }
    }
}
