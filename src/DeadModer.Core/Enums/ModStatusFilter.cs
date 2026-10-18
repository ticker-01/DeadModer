namespace DeadModer.Core.Enums;

/// <summary>
/// Фильтр по статусу установки мода.
/// </summary>
public enum ModStatusFilter
{
    All = 0,            // Все моды
    Installed = 1,      // Только установленные
    NotInstalled = 2,   // Только не установленные
    Available = 2       // Доступные к установке (псевдоним)
}
