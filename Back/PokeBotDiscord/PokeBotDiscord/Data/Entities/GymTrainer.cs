using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class GymTrainer
{
    public int Id { get; set; }

    // Gym this trainer belongs to
    public int? GymId { get; set; }
    public Gym Gym { get; set; } = null!;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Order of this trainer within the gym (1 = first, higher = later)
    public int Order { get; set; } = 1;

    // Up to 6 Pokémon in the team (species + level). Nulls mean unused slots.
    public int? Pokemon1SpeciesId { get; set; }
    public int? Pokemon1Level { get; set; }

    public int? Pokemon2SpeciesId { get; set; }
    public int? Pokemon2Level { get; set; }

    public int? Pokemon3SpeciesId { get; set; }
    public int? Pokemon3Level { get; set; }

    public int? Pokemon4SpeciesId { get; set; }
    public int? Pokemon4Level { get; set; }

    public int? Pokemon5SpeciesId { get; set; }
    public int? Pokemon5Level { get; set; }

    public int? Pokemon6SpeciesId { get; set; }
    public int? Pokemon6Level { get; set; }

    // Rewards for defeating this trainer
    public int RewardMoney { get; set; } = 0;

    public int? RewardItemTypeId { get; set; }
    public ItemType? RewardItemType { get; set; }

    public int RewardItemQuantity { get; set; } = 0;
}
