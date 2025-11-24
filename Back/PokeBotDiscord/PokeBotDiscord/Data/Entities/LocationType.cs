using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class LocationType
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = String.Empty;

    public bool Enabled { get; set; } = false;
    public bool Hidden { get; set; } = true;

    public bool HasWildEncounters { get; set; } = false;
    public bool AccessToShop { get; set; } = false;
    public bool AccessToGym { get; set; } = false;
    public bool AccessToPokemonCenter { get; set; } = false;
    public bool AccessToPokemonLeague { get; set; } = false;

    public int? GymId { get; set; } = null;
    public Gym? Gym { get; set; } = null;

    public List<Location> Locations { get; set; } = new();
}
