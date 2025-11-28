namespace PokeBotDiscord.Data.Entities;

public class PlayerTutorialMissionProgress
{
    public long Id { get; set; }

    public long PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int TutorialMissionId { get; set; }
    public TutorialMission TutorialMission { get; set; } = null!;

    public bool Completed { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
