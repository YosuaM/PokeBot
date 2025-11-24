using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class PartyModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    public PartyModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("party", "Shows the active party of a trainer on this server")] 
    public async Task PartyAsync(IUser? targetUser = null)
    {
        if (Context.Guild is null)
        {
            var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
            await RespondAsync(msg, ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var language = _localizationService.GetGuildLanguage(guildId);

        var target = targetUser ?? Context.User;

        // Ensure the target user is a member of this guild
        var guildUser = Context.Guild.GetUser(target.Id);
        if (guildUser is null)
        {
            var notFound = _localizationService.GetString("Profile.TrainerNotFound", language);
            await RespondAsync(notFound, ephemeral: true);
            return;
        }

        var player = await _dbContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == target.Id);

        if (player is null)
        {
            var notFound = _localizationService.GetString("Profile.TrainerNotFound", language);
            await RespondAsync(notFound, ephemeral: true);
            return;
        }

        // Load active party Pokémon (InParty = true, Level >= 1)
        var party = await _dbContext.Set<PokemonInstance>()
            .Include(pi => pi.Species)
            .Where(pi => pi.PlayerId == player.Id && pi.InParty && pi.Level >= 1)
            .OrderBy(pi => pi.PokemonSpeciesId)
            .ToListAsync();

        var title = _localizationService.GetString("Party.Title", language);
        var memberLabel = _localizationService.GetString("Party.MemberLabel", language);
        var emptyText = _localizationService.GetString("Party.Empty", language);

        var avatarUrl = target.GetAvatarUrl() ?? target.GetDefaultAvatarUrl();

        var embedBuilder = new EmbedBuilder()
            .WithTitle(string.Format(title, target.Username))
            .WithThumbnailUrl(avatarUrl)
            .WithColor(Color.Purple);

        if (party.Count == 0)
        {
            embedBuilder.WithDescription(emptyText);
        }
        else
        {
            var lines = new List<string>();
            foreach (var pi in party)
            {
                var species = pi.Species;
                var icon = string.IsNullOrWhiteSpace(species.IconCode) ? string.Empty : species.IconCode + " ";
                var line = string.Format(memberLabel, icon, species.Code, pi.Level);
                lines.Add(line);
            }

            embedBuilder.WithDescription(string.Join("\n", lines));
        }

        await RespondAsync(embed: embedBuilder.Build(), ephemeral: false);
    }
}
