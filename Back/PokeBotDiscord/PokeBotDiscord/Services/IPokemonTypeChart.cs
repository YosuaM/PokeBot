using System.Collections.Generic;

namespace PokeBotDiscord.Services;

public interface IPokemonTypeChart
{
    decimal GetMultiplier(string attackerTypeCode, string defenderTypeCode);

    decimal GetMultiplier(string attackerTypeCode, IEnumerable<string> defenderTypeCodes);
}
