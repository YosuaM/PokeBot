namespace PokeBotDiscord.Data.Entities;

public class TutorialMission
{
    public int Id { get; set; }

    public int TutorialStepId { get; set; }
    public TutorialStep TutorialStep { get; set; } = null!;

    public int Order { get; set; }

    public string DescriptionKey { get; set; } = null!;

    public string ConditionCode { get; set; } = null!;

    public List<PlayerTutorialMissionProgress> PlayerProgress { get; set; } = new();
}
