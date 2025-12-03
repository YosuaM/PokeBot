using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;

namespace PokeBotDiscord.Services;

public class PokemonTypeChart : IPokemonTypeChart
{
    private readonly Dictionary<string, int> _codeToId;
    private readonly Dictionary<(int AttackerTypeId, int DefenderTypeId), decimal> _effectiveness;

    private PokemonTypeChart(
        Dictionary<string, int> codeToId,
        Dictionary<(int, int), decimal> effectiveness)
    {
        _codeToId = codeToId;
        _effectiveness = effectiveness;
    }

    public decimal GetMultiplier(string attackerTypeCode, string defenderTypeCode)
    {
        if (!_codeToId.TryGetValue(attackerTypeCode, out var atkId) ||
            !_codeToId.TryGetValue(defenderTypeCode, out var defId))
        {
            return 1m;
        }

        return _effectiveness.TryGetValue((atkId, defId), out var mult) ? mult : 1m;
    }

    public decimal GetMultiplier(string attackerTypeCode, IEnumerable<string> defenderTypeCodes)
    {
        decimal result = 1m;

        foreach (var defCode in defenderTypeCodes)
        {
            result *= GetMultiplier(attackerTypeCode, defCode);
        }

        return result;
    }

    public static async Task<PokemonTypeChart> CreateAsync(PokeBotDbContext dbContext)
    {
        var types = await dbContext.PokemonTypes.AsNoTracking().ToListAsync();
        var effectivenessRows = await dbContext.PokemonTypeEffectiveness.AsNoTracking().ToListAsync();

        var codeToId = types.ToDictionary(t => t.Code, t => t.Id);

        var effectiveness = new Dictionary<(int, int), decimal>();
        foreach (var row in effectivenessRows)
        {
            effectiveness[(row.AttackerTypeId, row.DefenderTypeId)] = row.Multiplier;
        }

        return new PokemonTypeChart(codeToId, effectiveness);
    }
}
