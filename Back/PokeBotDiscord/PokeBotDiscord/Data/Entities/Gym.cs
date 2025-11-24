using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class Gym
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = String.Empty;

    // Optional code used to map this gym's badge to an emoji in Discord
    [MaxLength(50)]
    public string? BadgeCode { get; set; }
}
