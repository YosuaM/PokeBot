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

    // Location where this gym is found (city / route). Optional during migration/initial setup
    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    // Whether this gym is currently open for challenges
    public bool IsOpen { get; set; } = true;

    // Trainers that can be fought in this gym
    public List<GymTrainer> Trainers { get; set; } = new();
}
