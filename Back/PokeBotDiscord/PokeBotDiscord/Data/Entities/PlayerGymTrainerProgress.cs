namespace PokeBotDiscord.Data.Entities;

public class PlayerGymTrainerProgress
{
    public long Id { get; set; }

    public long PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int GymTrainerId { get; set; }
    public GymTrainer GymTrainer { get; set; } = null!;

    // Whether this trainer has been defeated at least once by this player
    public bool Defeated { get; set; } = false;

    // When the trainer was first defeated (UTC)
    public DateTime? FirstDefeatedAtUtc { get; set; }
}
