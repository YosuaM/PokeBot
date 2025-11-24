namespace PokeBotDiscord.Data.Entities;

public class PlayerGymBadge
{
    public long Id { get; set; }

    public long PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int GymId { get; set; }
    public Gym Gym { get; set; } = null!;

    // When the badge was obtained (UTC)
    public DateTime ObtainedAtUtc { get; set; } = DateTime.UtcNow;
}
