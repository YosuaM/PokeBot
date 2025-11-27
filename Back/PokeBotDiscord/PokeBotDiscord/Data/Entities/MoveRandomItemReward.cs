using System.ComponentModel.DataAnnotations.Schema;

namespace PokeBotDiscord.Data.Entities;

public class MoveRandomItemReward
{
    public int Id { get; set; }

    public int ItemTypeId { get; set; }
    public ItemType ItemType { get; set; } = null!;

    // Quantity range granted when this reward is picked
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;

    // Relative weight for random selection
    public int Weight { get; set; } = 1;
}
