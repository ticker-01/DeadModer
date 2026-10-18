using System;

namespace DeadModer.Core.Exceptions;

/// <summary>
/// Исключение при ошибках установки, распаковки или валидации файлов модификации.
/// </summary>
public class ModInstallationException : Exception
{
    public string? ModName { get; }
    public string? TargetPath { get; }

    public ModInstallationException(string message) 
        : base(message)
    {
    }

    public ModInstallationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public ModInstallationException(string modName, string targetPath, string message, Exception? innerException = null) 
        : base(message, innerException)
    {
        ModName = modName;
        TargetPath = targetPath;
    }
}
