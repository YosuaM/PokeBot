using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class PokemonType
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty; // e.g. FIRE, WATER

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // e.g. Fire, Water

    public List<PokemonSpeciesType> SpeciesTypes { get; set; } = new();

    public List<PokemonTypeEffectiveness> AttackingEffectiveness { get; set; } = new();

    public List<PokemonTypeEffectiveness> DefendingEffectiveness { get; set; } = new();
}
