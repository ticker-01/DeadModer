namespace DeadModer.Core.Enums;

/// <summary>
/// Фильтр по периоду добавления/обновления мода.
/// </summary>
public enum ModTimePeriod
{
    AllTime = 0,        // За всё время
    Day24h = 1,         // За 24 часа
    Week = 2,           // За неделю
    Month = 3           // За месяц
}
