using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;

namespace PokeBotDiscord;

public class ProfileModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;

    public ProfileModule(PokeBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [SlashCommand("profile", "Shows your trainer profile for this server")]
    public async Task ProfileAsync()
    {
        if (Context.Guild is null)
        {
            await RespondAsync("This command can only be used inside a server.", ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var userId = Context.User.Id;

        var player = await _dbContext.Players
            .Include(p => p.Party)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            player = new Player
            {
                GuildId = guildId,
                DiscordUserId = userId,
                CurrentLocationId = 1,
                Money = 0,
                LastTurnAtUtc = DateTime.UtcNow,
                TurnCredits = 0
            };

            _dbContext.Players.Add(player);
            await _dbContext.SaveChangesAsync();
        }

        var partyCount = player.Party?.Count ?? 0;

        var embed = new EmbedBuilder()
            .WithTitle("Trainer Profile")
            .WithColor(Color.Gold)
            .AddField("Server", Context.Guild.Name, inline: false)
            .AddField("Trainer", Context.User.Mention, inline: false)
            .AddField("Location", player.CurrentLocationId, inline: true)
            .AddField("Money", player.Money.ToString(), inline: true)
            .AddField("Party size", partyCount.ToString(), inline: true)
            .AddField("Last turn", player.LastTurnAtUtc.ToString("u"), inline: false)
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}
