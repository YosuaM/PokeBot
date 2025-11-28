using System.Threading.Tasks;

namespace PokeBotDiscord.Services;

public interface ITutorialService
{
    Task CompleteMissionsAsync(ulong guildId, ulong discordUserId, string conditionCode);
}
