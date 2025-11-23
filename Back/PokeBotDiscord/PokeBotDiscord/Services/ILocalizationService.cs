namespace PokeBotDiscord.Services;

public interface ILocalizationService
{
    string GetString(string key, string languageCode);
    string GetGuildLanguage(ulong guildId);
    Task SetGuildLanguageAsync(ulong guildId, string languageCode);
}
