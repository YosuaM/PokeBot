using System.ComponentModel.DataAnnotations;

namespace PokeBotDiscord.Data.Entities;

public class PokemonRarityCatchRate
{
    public int Id { get; set; }

    public int PokemonRarityId { get; set; }
    public PokemonRarity PokemonRarity { get; set; } = null!;

    // e.g. POKE_BALL, GREAT_BALL, ULTRA_BALL, MASTER_BALL
    [MaxLength(50)]
    public string BallCode { get; set; } = string.Empty;

    // 0-100
    public int CatchRatePercent { get; set; } = 0;
}
