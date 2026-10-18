using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DeadModer.Core.Enums;

namespace DeadModer.Core.Entities;

/// <summary>
/// Сущность кэшированной информации о моде из онлайн-каталога (GameBanana, комьюнити-хабы).
/// Позволяет мгновенно отображать популярные моды и поиск без постоянных сетевых запросов.
/// </summary>
public partial class CachedModInfo : ObservableObject
{
    /// <summary>
    /// Уникальный строковый идентификатор удаленного мода (например, "gb_482910").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Название мода.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Краткое описание для карточки в каталоге.
    /// </summary>
    [ObservableProperty]
    private string _shortDescription = string.Empty;

    /// <summary>
    /// Полное описание мода / Changelog.
    /// </summary>
    [ObservableProperty]
    private string _fullDescription = string.Empty;

    /// <summary>
    /// Автор / команда разработки мода.
    /// </summary>
    [ObservableProperty]
    private string _author = string.Empty;

    /// <summary>
    /// Текущая опубликованная версия мода (например, "1.2.0").
    /// </summary>
    [ObservableProperty]
    private string _version = "1.0.0";

    /// <summary>
    /// Категория мода.
    /// </summary>
    [ObservableProperty]
    private ModCategory _category = ModCategory.Other;

    /// <summary>
    /// Имя персонажа Deadlock (если мод предназначен для конкретного героя).
    /// </summary>
    [ObservableProperty]
    private string? _targetHero;

    /// <summary>
    /// Количество загрузок (например, 12400 для "12.4k").
    /// </summary>
    [ObservableProperty]
    private int _downloadCount;

    /// <summary>
    /// Средний пользовательский рейтинг (например, 4.8 из 5.0).
    /// </summary>
    [ObservableProperty]
    private double _rating = 5.0;

    /// <summary>
    /// Размер скачиваемого архива в байтах.
    /// </summary>
    [ObservableProperty]
    private long _fileSizeBytes;

    /// <summary>
    /// Прямая ссылка на скачивание архива/VPK.
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ссылка на превью/обложку мода.
    /// </summary>
    [ObservableProperty]
    private string _previewImageUrl = string.Empty;

    /// <summary>
    /// Локальный путь к закэшированной миниатюре (после скачивания).
    /// </summary>
    [ObservableProperty]
    private string? _localThumbnailPath;

    /// <summary>
    /// Дата публикации или последнего обновления мода в удаленном источнике.
    /// </summary>
    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата и время помещения записи в локальный кэш.
    /// </summary>
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Флаг "Выбор редакции" / Featured mod с высоким рейтингом.
    /// </summary>
    [ObservableProperty]
    private bool _isFeatured;

    /// <summary>
    /// Флаг наличия готовых файлов для прямой установки в один клик.
    /// </summary>
    [ObservableProperty]
    private bool _hasFiles = true;

    /// <summary>
    /// Флаг контента для взрослых (NSFW / 18+).
    /// </summary>
    [ObservableProperty]
    private bool _isNsfw;

    /// <summary>
    /// Источник/сайт публикации ("GameBanana", "Deadlocker").
    /// </summary>
    [ObservableProperty]
    private string _sourceSite = "GameBanana";

    /// <summary>
    /// Теги мода (например, "Voicelines", "Model", "Texture").
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Дополнительные скриншоты мода.
    /// </summary>
    public List<string> ScreenshotUrls { get; set; } = [];

    /// <summary>
    /// Форматированная строка количества загрузок (например, "12.4k").
    /// </summary>
    public string FormattedDownloads =>
        DownloadCount >= 1000
            ? $"{DownloadCount / 1000.0:F1}k"
            : DownloadCount.ToString();

    /// <summary>
    /// Человекочитаемый размер (например, "5.1 MB").
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (FileSizeBytes < 1024)
                return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024)
                return $"{FileSizeBytes / 1024.0:F1} KB";
            if (FileSizeBytes < 1024 * 1024 * 1024)
                return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";

            return $"{FileSizeBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}
