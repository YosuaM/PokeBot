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
            .Include(p => p.CurrentLocation)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var partyCount = player.Party?.Count ?? 0;

        // Localized strings
        var title = _localizationService.GetString("Profile.Title", language);
        var serverLabel = _localizationService.GetString("Profile.ServerLabel", language);
        var trainerLabel = _localizationService.GetString("Profile.TrainerLabel", language);
        var locationLabel = _localizationService.GetString("Profile.LocationLabel", language);
        var pokedexLabel = _localizationService.GetString("Profile.PokedexLabel", language);
        var badgesLabel = _localizationService.GetString("Profile.BadgesLabel", language);
        var badgesNone = _localizationService.GetString("Profile.BadgesNone", language);
        var moneyLabel = _localizationService.GetString("Profile.MoneyLabel", language);
        var staminaLabel = _localizationService.GetString("Profile.StaminaLabel", language);

        // Location name (localized by location code if available)
        string locationName;
        if (player.CurrentLocation is not null && !string.IsNullOrWhiteSpace(player.CurrentLocation.Code))
        {
            var locationKey = $"Locations.{player.CurrentLocation.Code}.Name";
            var localized = _localizationService.GetString(locationKey, language);
            locationName = localized == locationKey ? player.CurrentLocation.Code : localized;
        }
        else
        {
            locationName = player.CurrentLocationId.ToString();
        }

        // Pokédex: distinct species in party / total enabled species
        if (player.Party != null)
        {
            var capturedSpeciesCount = player.Party
                .Select(pi => pi.PokemonSpeciesId)
                .Distinct()
                .Count();

            var totalSpecies = await _dbContext.PokemonSpecies
                .AsNoTracking()
                .CountAsync(s => s.Enabled);

            var pokedexValue = $"{capturedSpeciesCount} / {totalSpecies}";

            // Badges: placeholder for now
            var badgesValue = badgesNone;

            var staminaValue = $"{player.CurrentStamina} / {player.MaxStamina}";

            // User avatar thumbnail on the right
            var avatarUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl();

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(Color.Gold)
                .WithThumbnailUrl(avatarUrl)
                .AddField(serverLabel, Context.Guild.Name, inline: false)
                .AddField(trainerLabel, Context.User.Mention, inline: false)
                .AddField(locationLabel, locationName, inline: false)
                .AddField(pokedexLabel, pokedexValue, inline: false)
                .AddField(badgesLabel, badgesValue, inline: false)
                .AddField(moneyLabel, player.Money.ToString(), inline: false)
                .AddField(staminaLabel, staminaValue, inline: false)
                .Build();

            await RespondAsync(embed: embed, ephemeral: false);
        }
    }
}
