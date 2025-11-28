namespace PokeBotDiscord.Data.Entities;

public class PlayerTutorialStepProgress
{
    public long Id { get; set; }

    public long PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int TutorialStepId { get; set; }
    public TutorialStep TutorialStep { get; set; } = null!;

    public bool RewardClaimed { get; set; }
    public DateTime? RewardClaimedAtUtc { get; set; }
}
