namespace PokeBotDiscord.Data.Entities;

public class GuildSettings
{
    public ulong GuildId { get; set; }
    public string Language { get; set; } = "en";
}
