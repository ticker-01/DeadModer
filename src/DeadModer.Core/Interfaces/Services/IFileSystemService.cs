using System;
using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Entities;
using DeadModer.Core.Models;

namespace DeadModer.Core.Interfaces.Services;

/// <summary>
/// Сервис файловой системы для установки, переключения состояния и удаления физических .vpk модов Source 2.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Устанавливает мод: проверяет/перемещает .vpk в папку game/citadel/addons и регистрирует запись в LiteDB.
    /// </summary>
    /// <param name="tempFilePath">Путь к скачанному или временному файлу (.vpk или .zip)</param>
    /// <param name="metadata">Метаданные модификации</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Зарегистрированная сущность установленного мода</returns>
    Task<InstalledMod> InstallModAsync(string tempFilePath, ModMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Переключает состояние активности мода в Source 2:
    /// Активен: {name}.vpk
    /// Отключен: {name}.vpk.disabled
    /// </summary>
    Task<bool> ToggleModStateAsync(string modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Переключает состояние активности мода по Guid.
    /// </summary>
    Task<bool> ToggleModStateAsync(Guid modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Полное удаление мода: удаляет физические файлы из addons и запись из LiteDB.
    /// </summary>
    Task<bool> UninstallModAsync(string modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Автоматически нормализует имена всех файлов в addons по стандарту Source 2 (pakXX_dir.vpk).
    /// </summary>
    Task MigrateNonPakFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Полное удаление мода по Guid.
    /// </summary>
    Task<bool> UninstallModAsync(Guid modId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Синхронная сигнатура установки мода.
    /// </summary>
    InstalledMod InstallMod(string tempFilePath, ModMetadata metadata);

    /// <summary>
    /// Синхронная сигнатура переключения активности мода.
    /// </summary>
    bool ToggleModState(Guid modId);

    /// <summary>
    /// Синхронная сигнатура удаления мода.
    /// </summary>
    bool UninstallMod(Guid modId);
}
