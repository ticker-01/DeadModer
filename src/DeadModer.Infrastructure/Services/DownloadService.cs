using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Exceptions;
using DeadModer.Core.Interfaces.Services;
using DeadModer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DeadModer.Infrastructure.Services;

/// <summary>
/// Универсальный сервис потокового скачивания файлов и распаковки .zip архивов "на лету".
/// Поддерживает точный расчет прогресса (IProgress&lt;double&gt;) и неблокирующую отмену.
/// </summary>
public sealed class DownloadService : IDownloadService
{
    private const int BufferSize = 81920; // 80 KB оптимальный размер буфера
    private readonly HttpClient _httpClient;
    private readonly ILogger<DownloadService>? _logger;

    public DownloadService(HttpClient httpClient, ILogger<DownloadService>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> DownloadFileAsync(
        string url, 
        string destinationPath, 
        IProgress<double>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var destinationDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        _logger?.LogInformation("Начало скачивания файла: {Url} -> {Dest}", url, destinationPath);

        // Используем ResponseHeadersRead для немедленного получения заголовков без буферизации всего тела в память
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        var canReportProgress = totalBytes.HasValue && totalBytes.Value > 0 && progress != null;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

        var buffer = new byte[BufferSize];
        long totalBytesRead = 0;
        int bytesRead;

        try
        {
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesRead += bytesRead;

                if (canReportProgress)
                {
                    var currentProgress = (double)totalBytesRead / totalBytes!.Value;
                    progress!.Report(Math.Clamp(currentProgress, 0.0, 1.0));
                }
            }

            // Финализируем 100% прогресс
            progress?.Report(1.0);
            _logger?.LogInformation("Файл успешно скачан ({TotalBytes} байт): {Dest}", totalBytesRead, destinationPath);
            return destinationPath;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Скачивание файла отменено пользователем: {Url}", url);
            fileStream.Close();
            TryDeleteFile(destinationPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Ошибка при скачивании файла: {Url}", url);
            fileStream.Close();
            TryDeleteFile(destinationPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DownloadedPackageInfo> DownloadAndUnpackAsync(
        string downloadUrl, 
        string? customSubDirName = null, 
        IProgress<double>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);

        var tempBase = Path.Combine(Path.GetTempPath(), "DeadModer");
        var downloadsDir = Path.Combine(tempBase, "Downloads");
        Directory.CreateDirectory(downloadsDir);

        var fileId = Guid.NewGuid().ToString("N");
        var inferredExt = InferExtension(downloadUrl);
        var tempDownloadedFile = Path.Combine(downloadsDir, $"{fileId}{inferredExt}");

        // 1. Потоковое скачивание файла во временную папку
        await DownloadFileAsync(downloadUrl, tempDownloadedFile, progress, cancellationToken);

        var fileInfo = new FileInfo(tempDownloadedFile);
        var isZip = IsZipArchive(tempDownloadedFile);

        if (isZip)
        {
            _logger?.LogInformation("Обнаружен ZIP архив. Распаковка 'на лету'...");
            var packageInfo = await UnpackArchiveAsync(tempDownloadedFile, null, cancellationToken);
            packageInfo.SourceUrl = downloadUrl;
            packageInfo.DownloadedFilePath = tempDownloadedFile;
            packageInfo.TotalBytes = fileInfo.Length;
            return packageInfo;
        }

        // Скачан чистый .vpk файл
        return new DownloadedPackageInfo
        {
            SourceUrl = downloadUrl,
            DownloadedFilePath = tempDownloadedFile,
            ExtractedDirectoryPath = null,
            VpkFilePaths = [tempDownloadedFile],
            IsArchive = false,
            TotalBytes = fileInfo.Length
        };
    }

    /// <inheritdoc />
    public Task<DownloadedPackageInfo> UnpackArchiveAsync(
        string archiveFilePath, 
        string? targetExtractDir = null, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveFilePath);

        if (!File.Exists(archiveFilePath))
        {
            throw new FileNotFoundException($"Архив не найден: {archiveFilePath}", archiveFilePath);
        }

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tempBase = Path.Combine(Path.GetTempPath(), "DeadModer", "Extracted");
            var extractDir = !string.IsNullOrWhiteSpace(targetExtractDir)
                ? targetExtractDir
                : Path.Combine(tempBase, Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(extractDir);

            try
            {
                _logger?.LogInformation("Распаковка архива {Archive} в {Dir}...", archiveFilePath, extractDir);

                bool extracted = false;
                try
                {
                    SharpCompress.Archives.ArchiveFactory.WriteToDirectory(archiveFilePath, extractDir, new SharpCompress.Common.ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                    extracted = true;
                }
                catch (Exception scEx)
                {
                    _logger?.LogDebug(scEx, "SharpCompress не смог открыть архив, попытка ZipFile...");
                }

                if (!extracted)
                {
                    ZipFile.ExtractToDirectory(archiveFilePath, extractDir, overwriteFiles: true);
                }

                // Рекурсивный поиск всех .vpk файлов внутри распакованного архива
                var foundVpkFiles = Directory.EnumerateFiles(extractDir, "*.vpk", SearchOption.AllDirectories).ToList();

                if (foundVpkFiles.Count == 0)
                {
                    throw new ModInstallationException(
                        Path.GetFileName(archiveFilePath), 
                        extractDir, 
                        "Скачанный архив не содержит ни одного файла формата .vpk");
                }

                _logger?.LogInformation("В архиве найдено {Count} VPK файлов", foundVpkFiles.Count);

                return new DownloadedPackageInfo
                {
                    SourceUrl = string.Empty,
                    DownloadedFilePath = archiveFilePath,
                    ExtractedDirectoryPath = extractDir,
                    VpkFilePaths = foundVpkFiles,
                    IsArchive = true,
                    TotalBytes = new FileInfo(archiveFilePath).Length
                };
            }
            catch (Exception ex) when (ex is not ModInstallationException)
            {
                _logger?.LogError(ex, "Ошибка при распаковке архива {Archive}", archiveFilePath);
                try { Directory.Delete(extractDir, true); } catch { /* ignore */ }
                throw new ModInstallationException(Path.GetFileName(archiveFilePath), extractDir, $"Не удалось распаковать архив: {ex.Message}", ex);
            }
        }, cancellationToken);
    }

    private static bool IsZipArchive(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is ".zip" or ".7z" or ".rar" or ".tar" or ".gz" or ".bz2") return true;

        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 4) return false;

            var header = new byte[6];
            var read = stream.Read(header, 0, Math.Min(header.Length, (int)stream.Length));
            if (read < 4) return false;

            // ZIP: PK\x03\x04
            if (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04) return true;
            // 7z: 7z\xBC\xAF\x27\x1C
            if (read >= 6 && header[0] == 0x37 && header[1] == 0x7A && header[2] == 0xBC && header[3] == 0xAF && header[4] == 0x27 && header[5] == 0x1C) return true;
            // RAR: Rar!
            if (header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && header[3] == 0x21) return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string InferExtension(string url)
    {
        try
        {
            var uri = new Uri(url);
            var ext = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrEmpty(ext)) return ext;
        }
        catch
        {
            // fallback
        }

        return ".bin";
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Игнорируем ошибки при очистке
        }
    }
}
