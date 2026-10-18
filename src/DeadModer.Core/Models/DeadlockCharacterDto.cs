using System.Collections.Generic;

namespace DeadModer.Core.Models;

/// <summary>
/// Информация о способности героя Deadlock.
/// </summary>
public class DeadlockAbilityDto
{
    public int Id { get; set; }
    public string Slot { get; set; } = "1";
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO персонажа Deadlock, автоматически парсируемого с Deadlocker / Deadlock API.
/// </summary>
public class DeadlockCharacterDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int NumericId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? IconUrl { get; set; }
    public string? Description { get; set; }
    public bool IsHeroLabs { get; set; }
    public int ModsCount { get; set; }
    public double Stamina { get; set; } = 3.0;
    public double MoveSpeed { get; set; } = 7.0;
    public int Health { get; set; } = 550;
    public string RoleType { get; set; } = "Assault";
    public List<DeadlockAbilityDto> Abilities { get; set; } = [];
}
