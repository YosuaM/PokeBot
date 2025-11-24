namespace PokeBotDiscord.Data.Entities;

public class PokemonInstance
{
    public long Id { get; set; }

    // Type reference
    public int PokemonSpeciesId { get; set; }
    public PokemonSpecies Species { get; set; } = null!;

    public int Level { get; set; } = 0;

    // Whether this Pokémon is currently in the owner's active party
    public bool InParty { get; set; } = true;

    // Player owner reference
    public long PlayerId { get; set; }
    public Player Owner { get; set; } = null!;
}
