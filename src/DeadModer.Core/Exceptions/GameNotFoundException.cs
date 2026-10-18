using System;

namespace DeadModer.Core.Exceptions;

/// <summary>
/// Исключение, возникающее, когда игра Deadlock не найдена в библиотеках Steam или по указанному пути.
/// </summary>
public class GameNotFoundException : Exception
{
    public GameNotFoundException() 
        : base("Игра Deadlock не найдена в библиотеках Steam. Убедитесь, что игра установлена, или укажите путь вручную в настройках.")
    {
    }

    public GameNotFoundException(string message) 
        : base(message)
    {
    }

    public GameNotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
