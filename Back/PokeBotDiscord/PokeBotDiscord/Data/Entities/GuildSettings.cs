using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class GuildSettings
{
    public ulong GuildId { get; set; }

    [MaxLength(3)]
    public string Language { get; set; } = "en";
}
