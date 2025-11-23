using Discord.Interactions;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class LanguageModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILocalizationService _localizationService;

    public LanguageModule(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    [SlashCommand("language", "Sets the bot language for this server")]
    public async Task SetLanguageAsync(string lang)
    {
        if (Context.Guild is null)
        {
            var message = _localizationService.GetString("Language.Set.ForGuildOnly", "en");
            await RespondAsync(message, ephemeral: true);
            return;
        }

        lang = lang.ToLowerInvariant();
        if (lang != "en" && lang != "es")
        {
            var invalid = _localizationService.GetString("Language.Set.Invalid", GetCurrentGuildLanguage());
            await RespondAsync(invalid, ephemeral: true);
            return;
        }

        await _localizationService.SetGuildLanguageAsync(Context.Guild.Id, lang);

        var key = lang switch
        {
            "es" => "Language.Set.Success.es",
            _ => "Language.Set.Success.en"
        };

        var confirm = _localizationService.GetString(key, lang);
        await RespondAsync(confirm, ephemeral: true);
    }

    private string GetCurrentGuildLanguage()
    {
        return Context.Guild is null
            ? "en"
            : _localizationService.GetGuildLanguage(Context.Guild.Id);
    }
}
