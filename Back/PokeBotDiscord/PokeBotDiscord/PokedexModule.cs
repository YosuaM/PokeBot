using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class PokedexModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;
    private readonly ITutorialService _tutorialService;

    private const int PageSize = 20;

    private const int FilterAll = 0;
    private const int FilterSeen = 1;
    private const int FilterOwned = 2;

    public PokedexModule(PokeBotDbContext dbContext, ILocalizationService localizationService, ITutorialService tutorialService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
        _tutorialService = tutorialService;
    }

    [SlashCommand("pokedex", "Shows a player's Pokédex for this server")] 
    public async Task PokedexAsync(IUser? targetUser = null)
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

        // Tutorial mission: POKEDEX_5_AND_CMD (only when viewing own Pokédex)
        if (target.Id == Context.User.Id)
        {
            var unlockedCount = await _dbContext.Set<PokemonInstance>()
                .Where(pi => pi.PlayerId == player.Id && pi.Level >= 0)
                .Select(pi => pi.PokemonSpeciesId)
                .Distinct()
                .CountAsync();

            if (unlockedCount >= 5)
            {
                await _tutorialService.CompleteMissionsAsync(guildId, Context.User.Id, "POKEDEX_5_AND_CMD");
            }
        }

        await RespondWithPokedexPageAsync(target, player, 0, FilterAll, language, ownerId: Context.User.Id);
    }

    [ComponentInteraction("pokedex_page:*:*:*:*")]
    public async Task HandlePokedexPageAsync(int page, int filter, ulong ownerId, ulong targetUserId)
    {
        if (Context.Guild is null)
        {
            var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
            await RespondAsync(msg, ephemeral: true);
            return;
        }

        if (Context.User.Id != ownerId)
        {
            await RespondAsync("You cannot change the Pokédex page for another user.", ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var language = _localizationService.GetGuildLanguage(guildId);

        var target = Context.Guild.GetUser(targetUserId) as IUser;
        if (target is null)
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

        await RespondWithPokedexPageAsync(target, player, page, filter, language, ownerId);
    }

    [ComponentInteraction("pokedex_filter:*:*:*")]
    public async Task HandlePokedexFilterAsync(int currentFilter, ulong ownerId, ulong targetUserId)
    {
        try
        {
            if (Context.Guild is null)
            {
                var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
                await RespondAsync(msg, ephemeral: true);
                return;
            }

            if (Context.User.Id != ownerId)
            {
                await RespondAsync("You cannot change the Pokédex filter for another user.", ephemeral: true);
                return;
            }

            var guildId = Context.Guild.Id;
            var language = _localizationService.GetGuildLanguage(guildId);

            var target = Context.Guild.GetUser(targetUserId) as IUser;
            if (target is null)
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

            // Cycle filter: ALL -> SEEN -> OWNED -> ALL
            var nextFilter = currentFilter switch
            {
                FilterAll => FilterSeen,
                FilterSeen => FilterOwned,
                FilterOwned => FilterAll,
                _ => FilterAll
            };

            await RespondWithPokedexPageAsync(target, player, 0, nextFilter, language, ownerId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PokedexModule.HandlePokedexFilterAsync] Error: {ex}");
            await RespondAsync("An error occurred while changing the Pokédex filter.", ephemeral: true);
        }
    }

    private async Task RespondWithPokedexPageAsync(IUser target, Player player, int page, int filter, string language, ulong ownerId)
    {
        // Load all enabled species and the player's instances
        var speciesList = await _dbContext.PokemonSpecies
            .Where(s => s.Enabled)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var instances = await _dbContext.Set<PokemonInstance>()
            .Where(pi => pi.PlayerId == player.Id)
            .ToListAsync();

        var totalSpecies = speciesList.Count;
        if (totalSpecies == 0)
        {
            var none = _localizationService.GetString("Pokedex.None", language);
            await RespondOrUpdateAsync(new EmbedBuilder().WithTitle("Pokédex").WithDescription(none).WithColor(Color.DarkGrey).Build(), null, ownerId, page, 0, 0, filter, language, target);
            return;
        }

        var unlockedIds = instances
            .Where(pi => pi.Level >= 0)
            .Select(pi => pi.PokemonSpeciesId)
            .Distinct()
            .ToHashSet();

        var ownedIds = instances
            .Where(pi => pi.Level >= 1)
            .Select(pi => pi.PokemonSpeciesId)
            .Distinct()
            .ToHashSet();

        // Apply filter to species list for pagination
        IEnumerable<PokemonSpecies> filteredSpecies = speciesList;
        switch (filter)
        {
            case FilterSeen:
                filteredSpecies = speciesList.Where(s => unlockedIds.Contains(s.Id));
                break;
            case FilterOwned:
                filteredSpecies = speciesList.Where(s => ownedIds.Contains(s.Id));
                break;
        }

        var filteredList = filteredSpecies.ToList();
        var totalFiltered = filteredList.Count;

        if (totalFiltered == 0)
        {
            var none = _localizationService.GetString("Pokedex.None", language);
            var noneEmbed = new EmbedBuilder()
                .WithTitle("Pokédex")
                .WithDescription(none)
                .WithColor(Color.DarkGrey)
                .Build();

            await RespondOrUpdateAsync(noneEmbed, null, ownerId, 0, 0, 0, filter, language, target);
            return;
        }

        var maxPage = (int)Math.Max(0, Math.Ceiling(totalFiltered / (double)PageSize) - 1);

        // Circular pagination: wrap around when going past ends
        if (page < 0)
        {
            page = maxPage;
        }
        else if (page > maxPage)
        {
            page = 0;
        }

        var startIndex = page * PageSize;
        var pageSpecies = filteredList.Skip(startIndex).Take(PageSize).ToList();

        var entries = new List<string>();

        foreach (var species in pageSpecies)
        {
            var instance = instances.FirstOrDefault(pi => pi.PokemonSpeciesId == species.Id);
            var number = species.Id;

            string line;
            if (instance is null)
            {
                // Locked: not discovered yet
                line = $"#{number:000}  :lock: ???";
            }
            else
            {
                // Unlocked: show icon and code, and level only when >= 1
                var icon = string.IsNullOrWhiteSpace(species.IconCode) ? string.Empty : species.IconCode + " ";
                if (instance.Level <= 0)
                {
                    line = $"#{number:000}  {icon}{species.Code}";
                }
                else
                {
                    line = $"#{number:000}  {icon}{species.Code} - Lv {instance.Level}";
                }
            }

            entries.Add(line);
        }

        var unlockedCount = instances
            .Where(pi => pi.Level >= 0)
            .Select(pi => pi.PokemonSpeciesId)
            .Distinct()
            .Count();

        var ownedCount = instances
            .Where(pi => pi.Level >= 1)
            .Select(pi => pi.PokemonSpeciesId)
            .Distinct()
            .Count();

        var titleTemplate = _localizationService.GetString("Pokedex.Title", language);
        var displayName = target.Username;
        var title = string.Format(titleTemplate, displayName);

        var desc = string.Join("\n", entries);

        var footerFmt = _localizationService.GetString("Pokedex.Footer", language);
        var footerText = string.Format(footerFmt, page + 1, maxPage + 1, unlockedCount, totalSpecies, ownedCount);

        var avatarUrl = target.GetAvatarUrl() ?? target.GetDefaultAvatarUrl();

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithThumbnailUrl(avatarUrl)
            .WithColor(Color.DarkBlue)
            .WithDescription(desc)
            .WithFooter(footerText)
            .Build();

        var components = BuildPaginationComponents(page, maxPage, filter, ownerId, target.Id, language);

        await RespondOrUpdateAsync(embed, components, ownerId, page, maxPage, totalSpecies, filter, language, target);
    }

    private MessageComponent BuildPaginationComponents(int page, int maxPage, int filter, ulong ownerId, ulong targetUserId, string language)
    {
        var previousLabel = _localizationService.GetString("Pokedex.PreviousPage", language);
        var nextLabel = _localizationService.GetString("Pokedex.NextPage", language);
        var filterLabelKey = filter switch
        {
            FilterSeen => "Pokedex.Filter.Seen",
            FilterOwned => "Pokedex.Filter.Owned",
            _ => "Pokedex.Filter.All"
        };

        var filterLabel = _localizationService.GetString(filterLabelKey, language);

        var builder = new ComponentBuilder();

        // If there is only one page, both buttons are disabled
        var singlePage = maxPage <= 0;

        builder.WithButton(previousLabel, $"pokedex_page:{page - 1}:{filter}:{ownerId}:{targetUserId}", ButtonStyle.Primary, disabled: singlePage);
        builder.WithButton(nextLabel, $"pokedex_page:{page + 1}:{filter}:{ownerId}:{targetUserId}", ButtonStyle.Primary, disabled: singlePage);
        builder.WithButton(filterLabel, $"pokedex_filter:{filter}:{ownerId}:{targetUserId}", ButtonStyle.Secondary);

        return builder.Build();
    }

    private async Task RespondOrUpdateAsync(Embed embed, MessageComponent? components, ulong ownerId, int page, int maxPage, int totalSpecies, int filter, string language, IUser target)
    {
        // For component interactions (pagination buttons), always edit the original message
        if (Context.Interaction is SocketMessageComponent component)
        {
            await component.UpdateAsync(msg =>
            {
                msg.Embed = embed;
                msg.Components = components ?? new ComponentBuilder().Build();
            });
            return;
        }

        // For the initial slash command, send a normal response
        await RespondAsync(embed: embed, components: components, ephemeral: false);
    }
}
