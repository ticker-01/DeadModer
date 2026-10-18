using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DeadModer.Core.Enums;

namespace DeadModer.Core.Entities;

/// <summary>
/// Сущность установленной модификации Deadlock.
/// Наследует ObservableObject для бесшовного биндинга динамических состояний (например, IsEnabled) в UI.
/// </summary>
public partial class InstalledMod : ObservableObject
{
    /// <summary>
    /// Уникальный идентификатор установленного мода (GUID или строковый ключ).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Идентификатор мода в удаленном каталоге (GameBanana ID / DeadModer Hub ID), если мод скачан из сети.
    /// </summary>
    public string? RemoteModId { get; set; }

    /// <summary>
    /// Название мода (например, "Better HUD", "No Minimap Fog").
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Краткое описание модификации.
    /// </summary>
    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Версия мода (например, "1.2.0").
    /// </summary>
    [ObservableProperty]
    private string _version = "1.0.0";

    /// <summary>
    /// Автор / создатель модификации.
    /// </summary>
    [ObservableProperty]
    private string _author = "Неизвестный автор";

    /// <summary>
    /// Категория мода (Gameplay, UI, Visual, Sounds, Other).
    /// </summary>
    [ObservableProperty]
    private ModCategory _category = ModCategory.Other;

    /// <summary>
    /// Конкретный герой Deadlock, для которого предназначен мод (null, если глобальный).
    /// </summary>
    [ObservableProperty]
    private string? _targetHero;

    /// <summary>
    /// Флаг включения/отключения мода.
    /// При false файлы мода временно деактивируются (переименовываются или выносятся из директории игры).
    /// </summary>
    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// Дата и время установки мода.
    /// </summary>
    public DateTime InstallDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего обновления файла мода.
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }

    /// <summary>
    /// Размер файлов мода в байтах.
    /// </summary>
    [ObservableProperty]
    private long _fileSizeBytes;

    /// <summary>
    /// Локальный путь к папке хранения мода в хранилище DeadModer.
    /// </summary>
    public string ModDirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Список установленных .vpk файлов и связанных ассетов относительно корня мода.
    /// </summary>
    public List<string> VpkFiles { get; set; } = [];

    /// <summary>
    /// Локальный путь к файлу превью / обложке мода.
    /// </summary>
    [ObservableProperty]
    private string? _localPreviewPath;

    /// <summary>
    /// URL оригинального изображения превью из удаленного источника.
    /// </summary>
    public string? RemotePreviewUrl { get; set; }

    /// <summary>
    /// Информация о совместимости (например, "Последняя версия игры").
    /// </summary>
    [ObservableProperty]
    private string _gameCompatibility = "Последняя версия игры";

    /// <summary>
    /// Теги для поиска и фильтрации.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Человекочитаемый размер файлов для отображения в инспекторе модов (например, "12.4 MB").
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
