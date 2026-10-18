namespace DeadModer.Core.Enums;

/// <summary>
/// Порядок сортировки модов в каталоге.
/// </summary>
public enum ModSortOrder
{
    Popular = 0,
    Popularity = 0, // Псевдоним для удобства
    Newest = 1,     // По новизне
    Rating = 2,     // По рейтингу
    Downloads = 3,  // По скачиваниям
    Name = 4        // По названию (A-Z)
}
