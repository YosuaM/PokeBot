namespace PokeBotDiscord.Data.Entities;

public class PokemonTypeEffectiveness
{
    public int Id { get; set; }

    public int AttackerTypeId { get; set; }
    public PokemonType AttackerType { get; set; } = null!;

    public int DefenderTypeId { get; set; }
    public PokemonType DefenderType { get; set; } = null!;

    // 0.00, 0.50, 1.00, 2.00, etc.
    public decimal Multiplier { get; set; } = 1m;
}
