using System;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Models;

namespace DeadModer.Core.Interfaces.Services;

/// <summary>
/// Универсальный сервис скачивания и распаковки файлов модификаций.
/// Поддерживает прогресс-бар (IProgress&lt;double&gt;) и отмену операции (CancellationToken).
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Скачивает файл по URL в указанное место назначения с отчетом о прогрессе (0.0 .. 1.0).
    /// </summary>
    /// <param name="url">URL скачиваемого файла</param>
    /// <param name="destinationPath">Локальный путь для сохранения</param>
    /// <param name="progress">Колбэк прогресса загрузки (0.0 .. 1.0)</param>
    /// <param name="cancellationToken">Токен отмены загрузки</param>
    /// <returns>Путь к скачанному файлу</returns>
    Task<string> DownloadFileAsync(
        string url, 
        string destinationPath, 
        IProgress<double>? progress = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Скачивает файл модификации во временную папку и распаковывает архив (.zip) "на лету",
    /// если файл упакован в архив. Возвращает готовые .vpk файлы для установки.
    /// </summary>
    /// <param name="downloadUrl">Прямой URL файла (.vpk или .zip)</param>
    /// <param name="customSubDirName">Опциональное имя подкаталога</param>
    /// <param name="progress">Колбэк прогресса загрузки (0.0 .. 1.0)</param>
    /// <param name="cancellationToken">Токен отмены загрузки</param>
    /// <returns>Пакет со списком найденных .vpk файлов</returns>
    Task<DownloadedPackageInfo> DownloadAndUnpackAsync(
        string downloadUrl, 
        string? customSubDirName = null, 
        IProgress<double>? progress = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Распаковывает локальный .zip архив во временную директорию и находит в нем все .vpk файлы.
    /// </summary>
    Task<DownloadedPackageInfo> UnpackArchiveAsync(
        string archiveFilePath, 
        string? targetExtractDir = null, 
        CancellationToken cancellationToken = default);
}
