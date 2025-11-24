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

    // Stamina system
    public DateTime LastTurnAtUtc { get; set; } = DateTime.UtcNow;
    public int MaxStamina { get; set; } = 5;
    public int CurrentStamina { get; set; } = 5;

    // Relationships
    public List<PokemonInstance> Party { get; set; } = new();
    public List<InventoryItem> Inventory { get; set; } = new();
    public List<PlayerGymBadge> GymBadges { get; set; } = new();
}
