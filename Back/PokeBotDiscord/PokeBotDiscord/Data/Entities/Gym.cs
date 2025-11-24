using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class Gym
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = String.Empty;
}
