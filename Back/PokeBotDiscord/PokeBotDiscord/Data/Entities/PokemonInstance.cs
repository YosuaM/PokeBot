namespace PokeBotDiscord.Data.Entities;

public class PokemonInstance
{
    public long Id { get; set; }

    // Type reference
    public int PokemonSpeciesId { get; set; }
    public PokemonSpecies Species { get; set; } = null!;

    public int Level { get; set; } = 0;

    // Player owner reference
    public long PlayerId { get; set; }
    public Player Owner { get; set; } = null!;
}
