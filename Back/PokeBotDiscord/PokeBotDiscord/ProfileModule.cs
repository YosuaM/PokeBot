using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class ProfileModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    public ProfileModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("profile", "Shows your trainer profile for this server")]
    public async Task ProfileAsync()
    {
        if (Context.Guild is null)
        {
            var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
            await RespondAsync(msg, ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var userId = Context.User.Id;
        var language = _localizationService.GetGuildLanguage(guildId);

        var player = await _dbContext.Players
            .Include(p => p.Party)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var partyCount = player.Party?.Count ?? 0;

        var title = _localizationService.GetString("Profile.Title", language);
        var serverLabel = _localizationService.GetString("Profile.ServerLabel", language);
        var trainerLabel = _localizationService.GetString("Profile.TrainerLabel", language);
        var locationLabel = _localizationService.GetString("Profile.LocationLabel", language);
        var moneyLabel = _localizationService.GetString("Profile.MoneyLabel", language);
        var partySizeLabel = _localizationService.GetString("Profile.PartySizeLabel", language);
        var lastTurnLabel = _localizationService.GetString("Profile.LastTurnLabel", language);

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Color.Gold)
            .AddField(serverLabel, Context.Guild.Name, inline: false)
            .AddField(trainerLabel, Context.User.Mention, inline: false)
            .AddField(locationLabel, player.CurrentLocationId, inline: true)
            .AddField(moneyLabel, player.Money.ToString(), inline: true)
            .AddField(partySizeLabel, partyCount.ToString(), inline: true)
            .AddField(lastTurnLabel, player.LastTurnAtUtc.ToString("u"), inline: false)
            .Build();

        await RespondAsync(embed: embed, ephemeral: false);
    }
}
