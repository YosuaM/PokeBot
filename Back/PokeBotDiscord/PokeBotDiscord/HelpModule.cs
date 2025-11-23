using Discord;
using Discord.Interactions;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILocalizationService _localizationService;

    public HelpModule(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    [SlashCommand("help", "Shows available commands")]
    public async Task HelpAsync()
    {
        var guildId = Context.Guild?.Id ?? 0;
        var language = guildId == 0 ? "en" : _localizationService.GetGuildLanguage(guildId);

        var title = _localizationService.GetString("Help.Title", language);
        var body = _localizationService.GetString("Help.Body", language);

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(body)
            .WithColor(Color.DarkBlue)
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}
