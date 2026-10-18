using System.Threading;
using System.Threading.Tasks;
using DeadModer.Core.Entities;

namespace DeadModer.Core.Interfaces.Data;

/// <summary>
/// Репозиторий для управления постоянными настройками приложения в LiteDB.
/// </summary>
public interface IAppSettingsRepository
{
    /// <summary>
    /// Загрузить текущие настройки приложения.
    /// Если настроек еще нет в базе, создает и возвращает настройки по умолчанию.
    /// </summary>
    Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохранить измененные настройки в локальную базу данных.
    /// </summary>
    Task<bool> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
