using System;

namespace DeadModer.Core.Exceptions;

/// <summary>
/// Исключение, возникающее, когда модификация с указанным ID не найдена в базе данных или на диске.
/// </summary>
public class ModNotFoundException : Exception
{
    public string ModId { get; }

    public ModNotFoundException(string modId) 
        : base($"Модификация с идентификатором '{modId}' не найдена.")
    {
        ModId = modId;
    }

    public ModNotFoundException(string modId, string message) 
        : base(message)
    {
        ModId = modId;
    }

    public ModNotFoundException(string modId, string message, Exception innerException) 
        : base(message, innerException)
    {
        ModId = modId;
    }
}
