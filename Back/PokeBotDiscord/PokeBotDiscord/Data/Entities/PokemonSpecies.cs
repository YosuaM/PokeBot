using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class PokemonSpecies
{
    // Pokedex number
    public int Id { get; set; }

    // Code name
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
}
