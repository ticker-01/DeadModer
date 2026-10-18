using System;
using System.Collections.Generic;
using System.IO;

namespace DeadModer.Core.Models;

/// <summary>
/// Информация о скачанном и распакованном пакете модификации во временной директории.
/// </summary>
public class DownloadedPackageInfo : IDisposable
{
    /// <summary>
    /// Исходный URL, откуда был скачан пакет.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Полный путь к скачанному временному файлу (.vpk или .zip).
    /// </summary>
    public string DownloadedFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Путь к временной папке, в которую был распакован архив (null, если был скачан чистый .vpk).
    /// </summary>
    public string? ExtractedDirectoryPath { get; set; }

    /// <summary>
    /// Список абсолютных путей ко всем найденным .vpk файлам, готовым для установки.
    /// </summary>
    public List<string> VpkFilePaths { get; set; } = [];

    /// <summary>
    /// Является ли скачанный файл архивом (.zip).
    /// </summary>
    public bool IsArchive { get; set; }

    /// <summary>
    /// Общий размер скачанных данных в байтах.
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Очистка временных файлов после завершения установки в addons.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(ExtractedDirectoryPath) && Directory.Exists(ExtractedDirectoryPath))
            {
                Directory.Delete(ExtractedDirectoryPath, recursive: true);
            }

            if (!string.IsNullOrWhiteSpace(DownloadedFilePath) && File.Exists(DownloadedFilePath))
            {
                File.Delete(DownloadedFilePath);
            }
        }
        catch
        {
            // Игнорируем исключения при удалении временных файлов ОС
        }
    }
}
