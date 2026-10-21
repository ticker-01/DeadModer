using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Models;

namespace DeadModer.Core.Interfaces.Services;

/// <summary>
/// Сервис обновления и восстановления совместимости Source 2 модов Deadlock (.vpk)
/// при выходе обновлений и патчей игры.
/// </summary>
public interface IModCompatibilityService
{
    /// <summary>
    /// Находит путь к ванильному pak01_dir.vpk игры Deadlock.
    /// </summary>
    Task<string> GetVanillaPakPathAsync(string? customGameDir = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет и при необходимости настраивает SearchPaths в gameinfo.gi для citadel/addons.
    /// </summary>
    Task<bool> EnsureGameInfoPatchedAsync(string? customGameDir = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет одиночный .vpk мод: удаляет устаревшие скрипты, обновляет .vnmskel_c и патчит .vmdl_c.
    /// </summary>
    Task<ModCompatibilityReport> UpdateSingleModVpkAsync(
        string modVpkPath,
        string vanillaPakPath,
        ISet<string> vanillaIndex,
        string backupDir,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Сканирует папку citadel/addons и обновляет все установленные pak*_dir.vpk моды.
    /// </summary>
    Task<IReadOnlyList<ModCompatibilityReport>> RunUpdaterAsync(
        string? customDeadlockDir = null,
        int maxWorkers = 8,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
