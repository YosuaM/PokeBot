namespace PokeBotDiscord.Data.Entities;

public class PokemonSpeciesType
{
    public int Id { get; set; }

    public int PokemonSpeciesId { get; set; }
    public PokemonSpecies Species { get; set; } = null!;

    public int PokemonTypeId { get; set; }
    public PokemonType Type { get; set; } = null!;
}
