using System;
using LiteDB;

namespace DeadModer.Infrastructure.Data;

/// <summary>
/// Контекст базы данных LiteDB.
/// Управляет жизненным циклом подключения к базе, сопоставлением BSON и индексами коллекций.
/// </summary>
public interface ILiteDbContext : IDisposable
{
    /// <summary>
    /// Экземпляр LiteDatabase для низкоуровневых операций или транзакций.
    /// </summary>
    ILiteDatabase Database { get; }

    /// <summary>
    /// Получить типизированную коллекцию сущностей.
    /// </summary>
    /// <typeparam name="T">Тип сущности</typeparam>
    /// <param name="name">Опциональное кастомное имя коллекции</param>
    ILiteCollection<T> GetCollection<T>(string? name = null);

    /// <summary>
    /// Инициализировать подключение, применить маппинг и создать индексы.
    /// </summary>
    void Initialize();
}
