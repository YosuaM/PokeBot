namespace PokeBotDiscord.Data.Entities;

public class PokemonEncounter
{
    public int Id { get; set; }

    public int PokemonSpeciesId { get; set; }
    public PokemonSpecies Species { get; set; } = null!;

    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;

    // Relative weight/probability for random selection within a location
    public int Weight { get; set; } = 1;

    // Optional level range for wild encounters
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 1;
}
