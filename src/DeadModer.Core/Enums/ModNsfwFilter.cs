namespace DeadModer.Core.Enums;

/// <summary>
/// Фильтр по возрастному ограничению / контенту (NSFW 18+).
/// </summary>
public enum ModNsfwFilter
{
    All = 0,            // Все моды
    SfwOnly = 1,        // Только безопасные (SFW)
    NsfwOnly = 2        // Только 18+ (NSFW)
}
