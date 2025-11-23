using Discord;
using Discord.Interactions;

namespace PokeBotDiscord;

public class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Shows available commands")]
    public async Task HelpAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("PokeBot Help")
            .WithDescription("/help — show this message")
            .WithColor(Color.DarkBlue)
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}
