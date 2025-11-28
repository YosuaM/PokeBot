namespace PokeBotDiscord.Data.Entities;

public class TutorialStep
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int Order { get; set; }

    public string TitleKey { get; set; } = null!;

    public string IntroKey { get; set; } = null!;

    public int RewardMoney { get; set; } = 0;

    public int? RewardItemTypeId { get; set; }
    public ItemType? RewardItemType { get; set; }

    public int RewardItemQuantity { get; set; } = 0;

    public List<TutorialMission> Missions { get; set; } = new();

    public List<PlayerTutorialStepProgress> PlayerProgress { get; set; } = new();
}
