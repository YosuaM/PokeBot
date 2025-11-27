using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class PokemonRarity
{
    public int Id { get; set; }

    [MaxLength(10)]
    public string Code { get; set; } = string.Empty; // N, R, SR, SRR, L, SL

    public int MinMoneyReward { get; set; } = 0;

    public int MaxMoneyReward { get; set; } = 0;

    public List<PokemonSpecies> Species { get; set; } = new();
    public List<PokemonRarityCatchRate> CatchRates { get; set; } = new();
}
