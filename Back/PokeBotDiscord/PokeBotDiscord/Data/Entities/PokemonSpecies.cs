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

    // Icon code (emoji)
    [MaxLength(50)]
    public string IconCode { get; set; } = string.Empty;

    // URL to a sprite image for this species
    [MaxLength(200)]
    public string SpriteUrl { get; set; } = string.Empty;

    // Rarity reference (nullable during migration/initial data setup)
    public int? PokemonRarityId { get; set; }
    public PokemonRarity? Rarity { get; set; }
}
