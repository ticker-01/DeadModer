using System;

namespace DeadModer.Core.Models;

/// <summary>
/// Отчет о проверке и восстановлении совместимости VPK мода с актуальной версией Deadlock.
/// </summary>
public sealed class ModCompatibilityReport
{
    /// <summary>
    /// Имя файла мода (например, pak01_dir.vpk).
    /// </summary>
    public string ModFileName { get; set; } = string.Empty;

    /// <summary>
    /// Полный путь к файлу мода на диске.
    /// </summary>
    public string ModFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Статус операции: UPDATED, OK (already up to date), SKIPPED (empty), ERROR.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Подробное сообщение о результате обработки.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Количество удаленных устаревших скриптов (scripts/*.vdata_c).
    /// </summary>
    public int StrippedScriptsCount { get; set; }

    /// <summary>
    /// Количество синхронизированных моделей (.vmdl_c).
    /// </summary>
    public int PatchedModelsCount { get; set; }

    /// <summary>
    /// Количество обновленных скелетов (.vnmskel_c).
    /// </summary>
    public int UpdatedSkeletonsCount { get; set; }

    /// <summary>
    /// Путь к сохраненной резервной копии оригинального файла.
    /// </summary>
    public string? BackupPath { get; set; }

    /// <summary>
    /// Был ли файл обновлен.
    /// </summary>
    public bool WasUpdated => string.Equals(Status, "UPDATED", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Завершилась ли обработка без критических ошибок.
    /// </summary>
    public bool IsSuccess => !Status.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase);
}
