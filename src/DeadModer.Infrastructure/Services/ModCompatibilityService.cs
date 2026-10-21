using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Interfaces.Services;
using DeadModer.Core.Models;
using Microsoft.Extensions.Logging;
using SteamDatabase.ValvePak;

namespace DeadModer.Infrastructure.Services;

/// <summary>
/// Реализация сервиса обновления и восстановления совместимости Source 2 модов Deadlock (.vpk)
/// при выходе обновлений и патчей игры.
/// Очищает устаревшие скрипты .vdata_c, обновляет скелеты .vnmskel_c и восстанавливает бинарные смещения моделей .vmdl_c.
/// </summary>
public sealed class ModCompatibilityService : IModCompatibilityService
{
    private static readonly string[] DefaultGamePaths =
    [
        @"D:\Program Files\Steam\steamapps\common\Deadlock",
        @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock",
        @"C:\Program Files\Steam\steamapps\common\Deadlock",
        @"D:\SteamLibrary\steamapps\common\Deadlock",
        @"E:\SteamLibrary\steamapps\common\Deadlock"
    ];

    private static readonly HashSet<string> StripPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "scripts/heroes.vdata_c",
        "scripts/abilities.vdata_c",
        "scripts/generic_data.vdata_c",
        "scripts/misc.vdata_c"
    };

    private static readonly byte[][] AttachmentKeys =
    [
        "camera"u8.ToArray(),
        "Camera"u8.ToArray(),
        "attach_camera"u8.ToArray(),
        "eye_pt"u8.ToArray(),
        "chest"u8.ToArray(),
        "healthbar"u8.ToArray()
    ];

    private readonly ISteamLocator _steamLocator;
    private readonly ILogger<ModCompatibilityService>? _logger;

    public ModCompatibilityService(ISteamLocator steamLocator, ILogger<ModCompatibilityService>? logger = null)
    {
        _steamLocator = steamLocator ?? throw new ArgumentNullException(nameof(steamLocator));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetVanillaPakPathAsync(string? customGameDir = null, CancellationToken cancellationToken = default)
    {
        var gameDir = await FindDeadlockDirAsync(customGameDir, cancellationToken);
        var pakPath = Path.Combine(gameDir, "game", "citadel", "pak01_dir.vpk");
        if (!File.Exists(pakPath))
        {
            throw new FileNotFoundException($"Базовый игровой архив pak01_dir.vpk не найден по пути: {pakPath}", pakPath);
        }
        return pakPath;
    }

    /// <inheritdoc />
    public async Task<bool> EnsureGameInfoPatchedAsync(string? customGameDir = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var gameDir = await FindDeadlockDirAsync(customGameDir, cancellationToken);
            var citadelDir = Path.Combine(gameDir, "game", "citadel");
            var giPath = Path.Combine(citadelDir, "gameinfo.gi");

            if (!File.Exists(giPath))
            {
                return false;
            }

            var content = await File.ReadAllTextAsync(giPath, Encoding.UTF8, cancellationToken);
            if (content.Contains("citadel/addons", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var pattern = new Regex(@"(SearchPaths\s*\{)", RegexOptions.IgnoreCase);
            var newContent = pattern.Replace(content, "$1\n\t\t\tGame\t\t\t\tcitadel/addons", 1);

            if (!string.Equals(content, newContent, StringComparison.Ordinal))
            {
                var bakPath = giPath + ".bak";
                try
                {
                    File.Copy(giPath, bakPath, overwrite: true);
                    await File.WriteAllTextAsync(giPath, newContent, Encoding.UTF8, cancellationToken);
                    _logger?.LogInformation("Успешно добавлены SearchPaths citadel/addons в gameinfo.gi");
                    return true;
                }
                catch (IOException ex)
                {
                    _logger?.LogWarning(ex, "gameinfo.gi заблокирован (возможно, игра запущена)");
                    return false;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Ошибка при проверке/настройке gameinfo.gi");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ModCompatibilityReport> UpdateSingleModVpkAsync(
        string modVpkPath,
        string vanillaPakPath,
        ISet<string> vanillaIndex,
        string backupDir,
        CancellationToken cancellationToken = default)
    {
        var modFileName = Path.GetFileName(modVpkPath);
        var report = new ModCompatibilityReport
        {
            ModFileName = modFileName,
            ModFilePath = modVpkPath
        };

        try
        {
            if (!File.Exists(modVpkPath))
            {
                report.Status = "ERROR";
                report.Message = "Файл мода не найден";
                return report;
            }

            if (!File.Exists(vanillaPakPath))
            {
                report.Status = "ERROR";
                report.Message = "Ванильный архив pak01_dir.vpk не найден";
                return report;
            }

            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extractedFiles = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

                // 1. Читаем все файлы из мода и СРАЗУ освобождаем файловый дескриптор Package,
                // чтобы исключить ошибку блокировки файла (Access to the path is denied) при последующей перезаписи.
                using (var modPak = new Package())
                {
                    try
                    {
                        modPak.Read(modVpkPath);
                    }
                    catch (Exception ex)
                    {
                        report.Status = "ERROR";
                        report.Message = $"Ошибка чтения VPK архива мода: {ex.Message}";
                        return report;
                    }

                    var allModEntries = modPak.Entries?.Values.SelectMany(x => x).ToList() ?? [];
                    if (allModEntries.Count == 0)
                    {
                        report.Status = "SKIPPED (empty)";
                        report.Message = "VPK архив мода пуст";
                        return report;
                    }

                    foreach (var entry in allModEntries)
                    {
                        modPak.ReadEntry(entry, out var rawData, validateCrc: false);
                        var normPath = entry.GetFullPath().Replace('\\', '/').ToLowerInvariant();
                        extractedFiles[normPath] = rawData;
                    }
                } // modPak гарантированно освобожден, блокировка с файла modVpkPath снята

                bool changed = false;
                var filesToKeep = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

                using (var vanillaPak = new Package())
                {
                    try
                    {
                        vanillaPak.Read(vanillaPakPath);
                    }
                    catch (Exception ex)
                    {
                        report.Status = "ERROR";
                        report.Message = $"Ошибка чтения ванильного архива: {ex.Message}";
                        return report;
                    }

                    foreach (var (normPath, rawDataOriginal) in extractedFiles)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var rawData = rawDataOriginal;

                        // 1. Удаление устаревших скриптов, вызывающих сбои после новых патчей Valve
                        if (StripPatterns.Contains(normPath) || (normPath.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase) && normPath.EndsWith(".vdata_c", StringComparison.OrdinalIgnoreCase)))
                        {
                            changed = true;
                            report.StrippedScriptsCount++;
                            continue;
                        }

                        // 2. Обновление скелетов (.vnmskel_c) напрямую из ванильного VPK при совпадении путей
                        if (normPath.EndsWith(".vnmskel_c", StringComparison.OrdinalIgnoreCase) && vanillaIndex.Contains(normPath))
                        {
                            var vanEntry = vanillaPak.FindEntry(normPath);
                            if (vanEntry != null)
                            {
                                vanillaPak.ReadEntry(vanEntry, out var vanData, validateCrc: false);
                                if (!rawData.AsSpan().SequenceEqual(vanData))
                                {
                                    rawData = vanData;
                                    changed = true;
                                    report.UpdatedSkeletonsCount++;
                                }
                            }
                        }
                        // 3. Безопасная синхронизация контроллеров моделей (.vmdl_c) против актуальных ванильных моделей
                        else if (normPath.EndsWith(".vmdl_c", StringComparison.OrdinalIgnoreCase) && vanillaIndex.Contains(normPath))
                        {
                            var vanEntry = vanillaPak.FindEntry(normPath);
                            if (vanEntry != null)
                            {
                                vanillaPak.ReadEntry(vanEntry, out var vanData, validateCrc: false);
                                var (patchedData, wasPatched) = SyncVmdlBinaryInPlace(rawData, vanData);
                                if (wasPatched)
                                {
                                    rawData = patchedData;
                                    changed = true;
                                    report.PatchedModelsCount++;
                                }
                            }
                        }

                        filesToKeep[normPath] = rawData;
                    }
                }

                if (!changed)
                {
                    report.Status = "OK (already up to date)";
                    report.Message = "Мод уже полностью актуален";
                    return report;
                }

                // Создание резервной копии оригинального файла перед перезаписью
                Directory.CreateDirectory(backupDir);
                var backupTarget = Path.Combine(backupDir, modFileName);
                if (!File.Exists(backupTarget))
                {
                    File.Copy(modVpkPath, backupTarget, overwrite: false);
                    report.BackupPath = backupTarget;
                }

                // Атомарная сборка нового VPK через временный файл
                var tempFile = Path.Combine(Path.GetTempPath(), $"mod_fix_{Guid.NewGuid():N}.vpk");
                try
                {
                    using (var newPak = new Package())
                    {
                        foreach (var (relPath, fileBytes) in filesToKeep)
                        {
                            newPak.AddFile(relPath, fileBytes);
                        }
                        newPak.Write(tempFile);
                    }

                    File.Move(tempFile, modVpkPath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }

                report.Status = "UPDATED";
                report.Message = $"Мод обновлен: удалено скриптов ({report.StrippedScriptsCount}), обновлено скелетов ({report.UpdatedSkeletonsCount}), адаптировано моделей ({report.PatchedModelsCount})";
                return report;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            report.Status = "ERROR";
            report.Message = ex.Message;
            _logger?.LogError(ex, "Ошибка обработки совместимости мода {ModFileName}", modFileName);
            return report;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModCompatibilityReport>> RunUpdaterAsync(
        string? customDeadlockDir = null,
        int maxWorkers = 8,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var gameDir = await FindDeadlockDirAsync(customDeadlockDir, cancellationToken);
        var citadelDir = Path.Combine(gameDir, "game", "citadel");
        var addonsDir = Path.Combine(citadelDir, "addons");
        var vanillaPakPath = Path.Combine(citadelDir, "pak01_dir.vpk");
        var backupDir = Path.Combine(citadelDir, "addons_backup");

        progress?.Report("Проверка SearchPaths в gameinfo.gi...");
        await EnsureGameInfoPatchedAsync(customDeadlockDir, cancellationToken);

        if (!Directory.Exists(addonsDir))
        {
            Directory.CreateDirectory(addonsDir);
            progress?.Report("Создана пустая папка addons.");
            return Array.Empty<ModCompatibilityReport>();
        }

        var modVpks = Directory.GetFiles(addonsDir, "pak*_dir.vpk*")
            .Where(f => f.EndsWith(".vpk", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".vpk.disabled", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (modVpks.Count == 0)
        {
            progress?.Report("Файлы pak*_dir.vpk в папке addons не найдены.");
            return Array.Empty<ModCompatibilityReport>();
        }

        progress?.Report($"Индексация ванильного {Path.GetFileName(vanillaPakPath)}...");
        var vanillaIndex = await Task.Run(() =>
        {
            using var vanillaPak = new Package();
            vanillaPak.Read(vanillaPakPath);
            var entries = vanillaPak.Entries?.Values
                .SelectMany(x => x)
                .Select(e => e.GetFullPath().Replace('\\', '/').ToLowerInvariant())
                ?? Enumerable.Empty<string>();
            return new HashSet<string>(entries, StringComparer.OrdinalIgnoreCase);
        }, cancellationToken);

        progress?.Report($"Проиндексировано {vanillaIndex.Count} файлов. Проверка {modVpks.Count} модов...");

        var results = new ConcurrentBag<ModCompatibilityReport>();
        int processedCount = 0;
        int total = modVpks.Count;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, maxWorkers),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(modVpks, parallelOptions, async (modVpk, ct) =>
        {
            var rep = await UpdateSingleModVpkAsync(modVpk, vanillaPakPath, vanillaIndex, backupDir, ct);
            results.Add(rep);
            var current = Interlocked.Increment(ref processedCount);
            progress?.Report($"[{rep.Status}] {rep.ModFileName} ({current}/{total})");
        });

        var orderedResults = results.OrderBy(r => r.ModFileName, StringComparer.OrdinalIgnoreCase).ToList();
        progress?.Report($"Завершено. Обновлено: {orderedResults.Count(r => r.WasUpdated)}, Без изменений: {orderedResults.Count(r => r.Status.StartsWith("OK"))}, Ошибок: {orderedResults.Count(r => !r.IsSuccess)}.");
        return orderedResults;
    }

    /// <summary>
    /// Парсит заголовки блоков Source 2 в компилированных ресурсах (.vmdl_c).
    /// </summary>
    public static Dictionary<string, (int Offset, int Size)> ParseS2Blocks(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16)
        {
            return new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        }

        // Заголовок Source 2: FileSize(uint), HeaderVersion(ushort), ResourceVersion(ushort), BlockOffset(uint), BlockCount(uint)
        uint fileSize = BinaryPrimitives.ReadUInt32LittleEndian(data[..4]);
        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(4, 2));
        ushort resourceVersion = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(6, 2));
        uint blockOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(8, 4));
        uint blockCount = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(12, 4));

        if (version != 12 || IsBlockCountInvalid(blockCount, data.Length))
        {
            return new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        }

        var blocks = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        int entryPos = 8 + (int)blockOffset;

        for (int i = 0; i < blockCount; i++)
        {
            if (entryPos + 12 > data.Length)
            {
                break;
            }

            string bType = Encoding.ASCII.GetString(data.Slice(entryPos, 4));
            int relOffset = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(entryPos + 4, 4));
            uint bSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(entryPos + 8, 4));
            int absOffset = (entryPos + 4) + relOffset;

            if (absOffset >= 0 && absOffset <= data.Length - (int)bSize)
            {
                blocks[bType] = (absOffset, (int)bSize);
            }

            entryPos += 12;
        }

        return blocks;
    }

    private static bool IsBlockCountInvalid(uint count, int totalLen)
    {
        return count == 0 || count > 64 || (count * 12 + 16) > (uint)totalLen;
    }

    /// <summary>
    /// Синхронизирует совместимые бинарные блоки управления Source 2 (CTRL) в компилированных моделях (.vmdl_c).
    /// </summary>
    public static (byte[] PatchedData, bool WasPatched) SyncVmdlBinaryInPlace(byte[] modBytes, byte[] vanillaBytes)
    {
        var modBlocks = ParseS2Blocks(modBytes);
        var vanBlocks = ParseS2Blocks(vanillaBytes);

        if (modBlocks.Count == 0 || vanBlocks.Count == 0)
        {
            return (modBytes, false);
        }

        byte[] buf = (byte[])modBytes.Clone();
        bool modified = false;

        // Безопасная синхронизация блока контроллеров костей (CTRL) при совпадении точного размера структуры
        if (modBlocks.TryGetValue("CTRL", out var modCtrl) && vanBlocks.TryGetValue("CTRL", out var vanCtrl))
        {
            if (modCtrl.Size == vanCtrl.Size && modCtrl.Size > 0)
            {
                var modCtrlSlice = buf.AsSpan(modCtrl.Offset, modCtrl.Size);
                var vanCtrlSlice = vanillaBytes.AsSpan(vanCtrl.Offset, vanCtrl.Size);
                if (!modCtrlSlice.SequenceEqual(vanCtrlSlice))
                {
                    vanCtrlSlice.CopyTo(modCtrlSlice);
                    modified = true;
                }
            }
        }

        return (buf, modified);
    }

    private async Task<string> FindDeadlockDirAsync(string? customPath, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            var pak = Path.Combine(customPath, "game", "citadel", "pak01_dir.vpk");
            if (File.Exists(pak))
            {
                return customPath;
            }
        }

        // Поиск через SteamLocator
        var steamGamePath = await _steamLocator.FindDeadlockGamePathAsync(cancellationToken);
        if (!string.IsNullOrEmpty(steamGamePath))
        {
            var pak = Path.Combine(steamGamePath, "game", "citadel", "pak01_dir.vpk");
            if (File.Exists(pak))
            {
                return steamGamePath;
            }
        }

        // Стандартные пути
        foreach (var path in DefaultGamePaths)
        {
            var pak = Path.Combine(path, "game", "citadel", "pak01_dir.vpk");
            if (File.Exists(pak))
            {
                return path;
            }
        }

        // Сканирование готовых дисков
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                var candidate = Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam", "steamapps", "common", "Deadlock");
                if (File.Exists(Path.Combine(candidate, "game", "citadel", "pak01_dir.vpk"))) return candidate;

                var candidate2 = Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "common", "Deadlock");
                if (File.Exists(Path.Combine(candidate2, "game", "citadel", "pak01_dir.vpk"))) return candidate2;
            }
        }
        catch { }

        throw new FileNotFoundException("Директория игры Deadlock не найдена. Укажите путь вручную в настройках.");
    }
}
