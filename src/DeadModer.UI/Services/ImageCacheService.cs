using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace DeadModer.UI.Services;

/// <summary>
/// Сервис кэширования и асинхронной загрузки изображений (обложек модов) с диска и сети.
/// Предотвращает повторные сетевые запросы и фризы UI.
/// </summary>
public static class ImageCacheService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly ConcurrentDictionary<string, Bitmap> MemoryCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Task<Bitmap?>> InFlightTasks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DeadModer",
        "Cache",
        "Images");

    static ImageCacheService()
    {
        try
        {
            Directory.CreateDirectory(CacheDirectory);
            if (!HttpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                HttpClient.DefaultRequestHeaders.Add("User-Agent", "DeadModer/1.0 ImageLoader");
            }
        }
        catch
        {
            // Игнорируем ошибки при создании папки кэша
        }
    }

    /// <summary>
    /// Асинхронно загружает изображение по URL или локальному пути с дисковым и оперативным кэшированием.
    /// Дедуплицирует параллельные запросы к одному и тому же URL.
    /// </summary>
    public static Task<Bitmap?> LoadBitmapAsync(string? urlOrPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath))
            return Task.FromResult<Bitmap?>(null);

        // 1. Проверяем кэш в оперативной памяти (0 мс)
        if (MemoryCache.TryGetValue(urlOrPath, out var cachedBitmap))
        {
            return Task.FromResult<Bitmap?>(cachedBitmap);
        }

        return InFlightTasks.GetOrAdd(urlOrPath, key => Task.Run(async () =>
        {
            try
            {
                return await LoadBitmapInternalAsync(key, cancellationToken);
            }
            finally
            {
                InFlightTasks.TryRemove(key, out _);
            }
        }, cancellationToken));
    }

    private static async Task<Bitmap?> LoadBitmapInternalAsync(string urlOrPath, CancellationToken cancellationToken)
    {
        try
        {
            // 2. Локальный ресурс Avalonia (avares://)
            if (urlOrPath.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(urlOrPath);
                    if (AssetLoader.Exists(uri))
                    {
                        using var stream = AssetLoader.Open(uri);
                        var bitmap = new Bitmap(stream);
                        MemoryCache.TryAdd(urlOrPath, bitmap);
                        return bitmap;
                    }
                }
                catch
                {
                    // Игнорируем и пробуем поиск на диске
                }

                // Запасной путь к локальным файлам на диске при отладке
                var cleanRelative = urlOrPath.Replace("avares://DeadModer.UI/", "").TrimStart('/');
                var candidatePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, cleanRelative),
                    Path.Combine(Directory.GetCurrentDirectory(), "src", "DeadModer.UI", cleanRelative),
                    Path.Combine(@"D:\Project\DeadModer\src\DeadModer.UI", cleanRelative)
                };

                foreach (var candidate in candidatePaths)
                {
                    if (File.Exists(candidate))
                    {
                        try
                        {
                            using var stream = File.OpenRead(candidate);
                            var bitmap = new Bitmap(stream);
                            MemoryCache.TryAdd(urlOrPath, bitmap);
                            return bitmap;
                        }
                        catch { }
                    }
                }
            }

            // 3. Локальный файл на диске
            if (File.Exists(urlOrPath))
            {
                using var stream = File.OpenRead(urlOrPath);
                var bitmap = new Bitmap(stream);
                MemoryCache.TryAdd(urlOrPath, bitmap);
                return bitmap;
            }

            // 4. Удаленный URL (HTTP / HTTPS)
            if (urlOrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                urlOrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var hash = ComputeHash(urlOrPath);
                var diskFilePath = Path.Combine(CacheDirectory, $"{hash}.img");

                // Проверяем наличие файла на диске
                if (File.Exists(diskFilePath))
                {
                    try
                    {
                        using var fileStream = File.OpenRead(diskFilePath);
                        var bitmap = new Bitmap(fileStream);
                        MemoryCache.TryAdd(urlOrPath, bitmap);
                        return bitmap;
                    }
                    catch
                    {
                        // Если файл поврежден, удаляем и скачиваем заново
                        File.Delete(diskFilePath);
                    }
                }

                // Скачиваем из сети
                var bytes = await HttpClient.GetByteArrayAsync(urlOrPath, cancellationToken);
                if (bytes == null || bytes.Length == 0)
                    return null;

                // Сохраняем на диск в кэш
                try
                {
                    await File.WriteAllBytesAsync(diskFilePath, bytes, cancellationToken);
                }
                catch
                {
                    // Игнорируем ошибку записи на диск, продолжим с памятью
                }

                using var ms = new MemoryStream(bytes);
                var downloadedBitmap = new Bitmap(ms);
                MemoryCache.TryAdd(urlOrPath, downloadedBitmap);
                return downloadedBitmap;
            }
        }
        catch
        {
            // Безопасно глушим ошибки сети, чтобы UI не падал при оффлайне или битых ссылках
        }

        return null;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
