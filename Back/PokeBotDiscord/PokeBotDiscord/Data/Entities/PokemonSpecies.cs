using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class PokemonSpecies
{
    // Pokedex number
    public int Id { get; set; }

    // Code name
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    // Whether this species can be selected as a starter
    public bool IsStarter { get; set; } = false;

    public bool Enabled { get; set; } = true;

    // Icon code
    [MaxLength(50)]
    public string IconCode { get; set; } = string.Empty;
}
