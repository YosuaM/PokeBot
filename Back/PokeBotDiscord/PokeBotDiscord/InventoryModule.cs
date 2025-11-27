using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class InventoryModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    private const int PageSize = 10;

    public InventoryModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("inventory", "Show a player's item inventory")] 
    public async Task InventoryAsync(IUser? targetUser = null)
    {
        try
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
                var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
                await RespondAsync(mustStart, ephemeral: true);
                return;
            }

            await RespondWithInventoryPageAsync(player, 0, language, ownerId: Context.User.Id, targetUserId: target.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InventoryModule.InventoryAsync] Error: {ex}");
            await RespondAsync("An error occurred while showing your inventory.", ephemeral: true);
        }
    }

    [ComponentInteraction("inventory_page:*:*:*")]
    public async Task HandleInventoryPageAsync(int page, ulong ownerId, ulong targetUserId)
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
                await RespondAsync("You cannot change the inventory page for another user.", ephemeral: true);
                return;
            }

            var guildId = Context.Guild.Id;
            var language = _localizationService.GetGuildLanguage(guildId);

            var player = await _dbContext.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == targetUserId);

            if (player is null)
            {
                var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
                await RespondAsync(mustStart, ephemeral: true);
                return;
            }

            await RespondWithInventoryPageAsync(player, page, language, ownerId, targetUserId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InventoryModule.HandleInventoryPageAsync] Error: {ex}");
            await RespondAsync("An error occurred while changing the inventory page.", ephemeral: true);
        }
    }

    private async Task RespondWithInventoryPageAsync(Player player, int page, string language, ulong ownerId, ulong targetUserId)
    {
        var items = await _dbContext.Set<InventoryItem>()
            .Include(ii => ii.ItemType)
            .Where(ii => ii.PlayerId == player.Id && ii.Quantity > 0)
            .OrderBy(ii => ii.ItemType.Code)
            .ToListAsync();

        var totalItems = items.Count;

        // Resolve target user display name and avatar
        var targetUser = Context.Guild?.GetUser(targetUserId) as IUser ?? Context.User;
        var displayName = targetUser.Username;
        var avatarUrl = targetUser.GetAvatarUrl() ?? targetUser.GetDefaultAvatarUrl();

        var titleTemplate = _localizationService.GetString("Inventory.Title", language);
        var title = string.Format(titleTemplate, displayName);

        if (totalItems == 0)
        {
            var empty = _localizationService.GetString("Inventory.Empty", language);
            var emptyEmbed = new EmbedBuilder()
                .WithTitle(title)
                .WithThumbnailUrl(avatarUrl)
                .WithDescription(empty)
                .WithColor(Color.DarkGrey)
                .Build();

            await RespondOrUpdateAsync(emptyEmbed, null, ownerId, page: 0, maxPage: 0, totalItems: 0, language: language, targetUserId: targetUserId);
            return;
        }

        var maxPage = (int)Math.Max(0, Math.Ceiling(totalItems / (double)PageSize) - 1);

        // Circular pagination
        if (page < 0)
        {
            page = maxPage;
        }
        else if (page > maxPage)
        {
            page = 0;
        }

        var startIndex = page * PageSize;
        var pageItems = items.Skip(startIndex).Take(PageSize).ToList();

        var rows = new List<string>();
        var rowTemplate = _localizationService.GetString("Inventory.Row", language);

        var index = startIndex + 1;
        foreach (var item in pageItems)
        {
            var icon = item.ItemType.IconCode;
            var code = item.ItemType.Code;

            var nameKey = $"Item.{code}.Name";
            var localizedName = _localizationService.GetString(nameKey, language);
            var name = string.IsNullOrEmpty(localizedName) || localizedName == nameKey
                ? code
                : localizedName;

            var line = string.Format(rowTemplate, index, icon, name, item.Quantity);
            rows.Add(line);
            index++;
        }

        var desc = string.Join("\n", rows);

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithThumbnailUrl(avatarUrl)
            .WithDescription(desc)
            .WithColor(Color.DarkBlue)
            .Build();

        var components = BuildPaginationComponents(page, maxPage, ownerId, targetUserId, language);

        await RespondOrUpdateAsync(embed, components, ownerId, page, maxPage, totalItems, language, targetUserId);
    }

    private MessageComponent BuildPaginationComponents(int page, int maxPage, ulong ownerId, ulong targetUserId, string language)
    {
        var previousLabel = _localizationService.GetString("Inventory.PreviousPage", language);
        var nextLabel = _localizationService.GetString("Inventory.NextPage", language);

        var builder = new ComponentBuilder();
        var singlePage = maxPage <= 0;

        builder.WithButton(previousLabel, $"inventory_page:{page - 1}:{ownerId}:{targetUserId}", ButtonStyle.Primary, disabled: singlePage);
        builder.WithButton(nextLabel, $"inventory_page:{page + 1}:{ownerId}:{targetUserId}", ButtonStyle.Primary, disabled: singlePage);

        return builder.Build();
    }

    private async Task RespondOrUpdateAsync(Embed embed, MessageComponent? components, ulong ownerId, int page, int maxPage, int totalItems, string language, ulong targetUserId)
    {
        if (Context.Interaction is SocketMessageComponent component)
        {
            await component.UpdateAsync(msg =>
            {
                msg.Embed = embed;
                msg.Components = components ?? new ComponentBuilder().Build();
            });
            return;
        }

        await RespondAsync(embed: embed, components: components, ephemeral: false);
    }
}
