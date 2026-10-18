using System.Collections.Generic;
using DeadModer.Core.Enums;

namespace DeadModer.Core.Models;

/// <summary>
/// Метаданные устанавливаемой модификации (из архива, VPK или манифеста каталога).
/// </summary>
public class ModMetadata
{
    /// <summary>
    /// Название мода (например, "Better HUD", "No Minimap Fog").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Краткое описание мода.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Версия мода (по умолчанию 1.0.0).
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Автор / создатель.
    /// </summary>
    public string Author { get; set; } = "Неизвестный автор";

    /// <summary>
    /// Категория мода.
    /// </summary>
    public ModCategory Category { get; set; } = ModCategory.Other;

    /// <summary>
    /// Целевой персонаж Deadlock (если мод героя).
    /// </summary>
    public string? TargetHero { get; set; }

    /// <summary>
    /// Идентификатор мода в удаленном каталоге (GameBanana / DeadModer Hub).
    /// </summary>
    public string? RemoteModId { get; set; }

    /// <summary>
    /// Путь к файлу превью или URL изображения.
    /// </summary>
    public string? PreviewImagePath { get; set; }

    /// <summary>
    /// Совместимость с версией игры.
    /// </summary>
    public string GameCompatibility { get; set; } = "Последняя версия игры";

    /// <summary>
    /// Теги для поиска.
    /// </summary>
    public List<string> Tags { get; set; } = [];
}
