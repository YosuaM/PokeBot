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
    private readonly ITutorialService _tutorialService;

    public ProfileModule(PokeBotDbContext dbContext, ILocalizationService localizationService, ITutorialService tutorialService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
        _tutorialService = tutorialService;
    }

    private async Task<bool> RegenerateStaminaAsync(Player player)
    {
        if (player.CurrentStamina >= player.MaxStamina)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var last = player.LastTurnAtUtc;
        if (now <= last)
        {
            return false;
        }

        var elapsed = now - last;
        var fullHours = (int)Math.Floor(elapsed.TotalHours);
        if (fullHours <= 0)
        {
            return false;
        }

        var guildSettings = await _dbContext.GuildSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GuildId == player.GuildId);

        var staminaPerHour = guildSettings?.StaminaPerHour ?? 1;
        var staminaGain = fullHours * staminaPerHour;

        if (staminaGain > 0)
        {
            player.CurrentStamina = Math.Min(player.MaxStamina, player.CurrentStamina + staminaGain);
        }

        player.LastTurnAtUtc = last.AddHours(fullHours);

        return true;
    }

    [SlashCommand("profile", "Shows a trainer profile for this server")]
    public async Task ProfileAsync(IUser? targetUser = null)
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

        var userId = target.Id;

        var player = await _dbContext.Players
            .Include(p => p.Party)
            .Include(p => p.CurrentLocation)
            .Include(p => p.GymBadges)
            .ThenInclude(b => b.Gym)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var notFound = _localizationService.GetString("Profile.TrainerNotFound", language);
            await RespondAsync(notFound, ephemeral: true);
            return;
        }

        if (await RegenerateStaminaAsync(player))
        {
            await _dbContext.SaveChangesAsync();
        }

        // Tutorial: mission CMD_PROFILE (only when viewing own profile)
        if (target.Id == Context.User.Id)
        {
            await _tutorialService.CompleteMissionsAsync(guildId, Context.User.Id, "CMD_PROFILE");
        }

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

            // Badges: show emojis for each earned badge, or the localized "none" text
            string badgesValue;
            if (player.GymBadges != null && player.GymBadges.Count > 0)
            {
                var emojiCodes = player.GymBadges
                    .Where(b => b.Gym != null && !string.IsNullOrWhiteSpace(b.Gym.BadgeCode))
                    .Select(b =>
                    {
                        var code = b.Gym.BadgeCode!;

                        // If BadgeCode already contains full emoji markup (<:...:...>), use it as-is
                        if (code.StartsWith("<") && code.EndsWith(">"))
                        {
                            return code;
                        }

                        // Fallback: treat it as a plain emoji name
                        return $":{code}:";
                    })
                    .ToList();

                badgesValue = emojiCodes.Count > 0
                    ? string.Join(" ", emojiCodes)
                    : badgesNone;
            }
            else
            {
                badgesValue = badgesNone;
            }

            var staminaValue = $"{player.CurrentStamina} / {player.MaxStamina}";

            // User avatar thumbnail on the right
            var avatarUrl = target.GetAvatarUrl() ?? target.GetDefaultAvatarUrl();

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(Color.Gold)
                .WithThumbnailUrl(avatarUrl)
                .AddField(serverLabel, Context.Guild.Name, inline: false)
                .AddField(trainerLabel, target.Mention, inline: false)
                .AddField(locationLabel, locationName, inline: false)
                .AddField(pokedexLabel, pokedexValue, inline: false)
                .AddField(badgesLabel, badgesValue, inline: false)
                .AddField(moneyLabel, $"₽{player.Money}", inline: false)
                .AddField(staminaLabel, staminaValue, inline: false)
                .Build();

            await RespondAsync(embed: embed, ephemeral: false);
        }
    }
}
