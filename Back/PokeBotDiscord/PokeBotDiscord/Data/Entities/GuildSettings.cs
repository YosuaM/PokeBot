using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class GuildSettings
{
    public ulong GuildId { get; set; }

    [MaxLength(3)]
    public string Language { get; set; } = "en";

    // Stamina configuration: how many stamina points are recovered per hour for players in this guild
    public int StaminaPerHour { get; set; } = 1;

    // Chance (0-100) that a random item event triggers when using /move
    public int MoveItemEventChancePercent { get; set; } = 0;

    // Chance (0-100) that a surprise battle event triggers when using /move
    public int MoveBattleEventChancePercent { get; set; } = 0;

    // Money reward range for winning a surprise battle triggered by /move
    public int MoveBattleWinMinMoneyReward { get; set; } = 0;
    public int MoveBattleWinMaxMoneyReward { get; set; } = 0;
}
