using System.ComponentModel.DataAnnotations.Schema;

namespace PokeBotDiscord.Data.Entities;

public class Player
{
    public long Id { get; set; }

    // Discord context
    public ulong GuildId { get; set; }
    public ulong DiscordUserId { get; set; }

    // World state
    public int CurrentLocationId { get; set; } = 1;
    public Location CurrentLocation { get; set; } = null!;

    public int Money { get; set; } = 0;

    // Turn system
    public DateTime LastTurnAtUtc { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(18,1)")]
    public decimal TurnCredits { get; set; } = 0.0M;

    // Relationships
    public List<PokemonInstance> Party { get; set; } = new();
    public List<InventoryItem> Inventory { get; set; } = new();
}
