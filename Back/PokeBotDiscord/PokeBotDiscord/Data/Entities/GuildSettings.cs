using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class GuildSettings
{
    public ulong GuildId { get; set; }

    [MaxLength(3)]
    public string Language { get; set; } = "en";

    // Stamina configuration: how many stamina points are recovered per hour for players in this guild
    public int StaminaPerHour { get; set; } = 1;
}
