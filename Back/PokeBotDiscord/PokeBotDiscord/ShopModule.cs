using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class ShopModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    public ShopModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("shop", "Open the shop in your current location")] 
    public async Task ShopAsync()
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

            var player = await _dbContext.Players
                .Include(p => p.CurrentLocation)
                    .ThenInclude(l => l.LocationType)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == Context.User.Id);

            if (player is null)
            {
                var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
                await RespondAsync(mustStart, ephemeral: true);
                return;
            }

            var location = player.CurrentLocation;
            var locationType = location.LocationType;

            if (!locationType.AccessToShop)
            {
                var noShop = _localizationService.GetString("Shop.NoShopHere", language);
                await RespondAsync(noShop, ephemeral: true);
                return;
            }

            // Find stores assigned to this specific location
            var stores = await _dbContext.LocationStores
                .Include(ls => ls.StoreType)
                .Where(ls => ls.LocationId == location.Id && ls.StoreType.Enabled)
                .OrderBy(ls => ls.SortOrder)
                .ToListAsync();

            if (stores.Count == 0)
            {
                var noShop = _localizationService.GetString("Shop.NoShopHere", language);
                await RespondAsync(noShop, ephemeral: true);
                return;
            }

            // For now, just open the first store configured for this location
            var store = stores[0];
            var storeType = store.StoreType;

            var items = await _dbContext.StoreTypeItems
                .Include(sti => sti.ItemType)
                .Where(sti => sti.StoreTypeId == storeType.Id && sti.Enabled)
                .OrderBy(sti => sti.SortOrder)
                .ToListAsync();

            var titleTemplate = _localizationService.GetString("Shop.Title", language);
            var emptyText = _localizationService.GetString("Shop.Empty", language);
            var itemRowTemplate = _localizationService.GetString("Shop.ItemRow", language);

            var locationName = location.Code;
            var title = string.Format(titleTemplate, locationName);

            var embedBuilder = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(Color.DarkBlue);

            if (items.Count == 0)
            {
                embedBuilder.WithDescription(emptyText);
                await RespondAsync(embed: embedBuilder.Build(), ephemeral: false);
                return;
            }

            var lines = new List<string>();
            var index = 1;
            var componentBuilder = new ComponentBuilder();

            var ownerId = Context.User.Id;
            var locationId = location.Id;

            foreach (var i in items)
            {
                var icon = i.ItemType.IconCode; // e.g. <:poke_ball:...>
                var code = i.ItemType.Code;

                // Localized display name for the item, fallback to code if not found
                var nameKey = $"Item.{code}.Name";
                var localizedName = _localizationService.GetString(nameKey, language);
                var name = string.IsNullOrEmpty(localizedName) || localizedName == nameKey
                    ? code
                    : localizedName;

                var owned = player.Inventory.FirstOrDefault(ii => ii.ItemTypeId == i.ItemTypeId)?.Quantity ?? 0;
                var line = string.Format(itemRowTemplate, index, icon, name, i.Price, owned);
                lines.Add(line);

                // Button per item: show only the item emoji (no text label)
                if (!string.IsNullOrWhiteSpace(icon) && Emote.TryParse(icon, out var emote))
                {
                    // Discord requires label length >= 1, so use a zero-width space to keep it visually empty
                    componentBuilder.WithButton(
                        label: "\u200B",
                        customId: $"shop_buy:{i.Id}:{ownerId}:{locationId}",
                        style: ButtonStyle.Primary,
                        emote: emote);
                }
                else
                {
                    // Fallback: text label if icon is missing/invalid
                    componentBuilder.WithButton(
                        label: name,
                        customId: $"shop_buy:{i.Id}:{ownerId}:{locationId}",
                        style: ButtonStyle.Primary);
                }

                index++;
            }

            embedBuilder.WithDescription(string.Join("\n", lines));

            // Show player's current money
            var moneyLabel = _localizationService.GetString("Profile.MoneyLabel", language);
            embedBuilder.AddField(moneyLabel, $"₽{player.Money}", inline: false);

            await RespondAsync(embed: embedBuilder.Build(), components: componentBuilder.Build(), ephemeral: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShopModule.ShopAsync] Error: {ex}");
            await RespondAsync("An error occurred while opening the shop.", ephemeral: true);
        }
    }

    [ComponentInteraction("shop_buy:*:*:*")]
    public async Task OpenShopQuantityModalAsync(int storeItemId, ulong ownerId, int locationId)
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

            if (Context.User.Id != ownerId)
            {
                var notYourShop = _localizationService.GetString("Shop.NotYourShop", language);
                await RespondAsync(notYourShop, ephemeral: true);
                return;
            }

            // Ensure the player is still in the same location as when the shop was opened
            var player = await _dbContext.Players
                .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == ownerId);

            if (player is null || player.CurrentLocationId != locationId)
            {
                var wrongLocation = _localizationService.GetString("Shop.NotInThisLocation", language);
                await RespondAsync(wrongLocation, ephemeral: true);
                return;
            }

            // Open a modal asking for the quantity to buy
            await RespondWithModalAsync<ShopQuantityModal>($"shop_qty:{storeItemId}:{ownerId}:{locationId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShopModule.OpenShopQuantityModalAsync] Error: {ex}");
            await RespondAsync("An error occurred while preparing the purchase.", ephemeral: true);
        }
    }

    [ComponentInteraction("shop_confirm:*:*:*")]
    public async Task HandleShopConfirmAsync(int storeItemId, int quantity, ulong ownerId)
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
                await RespondAsync("You cannot confirm a purchase for another user.", ephemeral: true);
                return;
            }

            var guildId = Context.Guild.Id;
            var language = _localizationService.GetGuildLanguage(guildId);

            var player = await _dbContext.Players
                .Include(p => p.Inventory)
                .Include(p => p.CurrentLocation)
                .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == ownerId);

            if (player is null)
            {
                var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
                await RespondAsync(mustStart, ephemeral: true);
                return;
            }

            var storeItem = await _dbContext.StoreTypeItems
                .Include(sti => sti.ItemType)
                .Include(sti => sti.StoreType)
                .FirstOrDefaultAsync(sti => sti.Id == storeItemId && sti.Enabled);

            if (storeItem is null)
            {
                var unavailable = _localizationService.GetString("Shop.ItemUnavailable", language);
                await RespondAsync(unavailable, ephemeral: true);
                return;
            }

            if (quantity <= 0)
            {
                var invalidQty = _localizationService.GetString("Shop.InvalidQuantity", language);
                await RespondAsync(invalidQty, ephemeral: true);
                return;
            }

            var totalPrice = storeItem.Price * quantity;
            if (player.Money < totalPrice)
            {
                var notEnough = _localizationService.GetString("Shop.NotEnoughMoney", language);
                await RespondAsync(notEnough, ephemeral: true);
                if (Context.Interaction is SocketMessageComponent notEnoughComponent)
                {
                    await notEnoughComponent.UpdateAsync(msg =>
                    {
                        msg.Components = new ComponentBuilder().Build();
                    });
                }
                return;
            }

            // Deduct money
            player.Money -= totalPrice;

            // Upsert inventory item
            var existing = player.Inventory
                .FirstOrDefault(ii => ii.ItemTypeId == storeItem.ItemTypeId);

            if (existing is null)
            {
                existing = new InventoryItem
                {
                    PlayerId = player.Id,
                    ItemTypeId = storeItem.ItemTypeId,
                    Quantity = quantity
                };
                _dbContext.Add(existing);
            }
            else
            {
                existing.Quantity += quantity;
            }

            await _dbContext.SaveChangesAsync();

            var code = storeItem.ItemType.Code;
            var icon = storeItem.ItemType.IconCode;
            var nameKey = $"Item.{code}.Name";
            var localizedName = _localizationService.GetString(nameKey, language);
            var name = string.IsNullOrEmpty(localizedName) || localizedName == nameKey
                ? code
                : localizedName;

            var successTemplate = _localizationService.GetString("Shop.PurchaseSuccess", language);

            var storeTypeCode = storeItem.StoreType?.Code ?? "CITY_SHOP";
            var storeNameKey = $"StoreType.{storeTypeCode}.Name";
            var localizedStoreName = _localizationService.GetString(storeNameKey, language);
            var shopName = string.IsNullOrEmpty(localizedStoreName) || localizedStoreName == storeNameKey
                ? storeTypeCode
                : localizedStoreName;

            var locationCode = player.CurrentLocation?.Code ?? "Unknown";
            var locationNameKey = $"Locations.{locationCode}.Name";
            var localizedLocationName = _localizationService.GetString(locationNameKey, language);
            var locationName = string.IsNullOrEmpty(localizedLocationName) || localizedLocationName == locationNameKey
                ? locationCode
                : localizedLocationName;
            var successText = string.Format(successTemplate,
                Context.User.Mention,
                quantity,
                icon,
                name,
                totalPrice,
                shopName,
                locationName);

            // Public announcement in the channel so everyone sees the purchase
            await Context.Channel.SendMessageAsync(successText);

            // Also clear buttons on the original (ephemeral) confirmation message
            if (Context.Interaction is SocketMessageComponent component)
            {
                await component.UpdateAsync(msg =>
                {
                    msg.Components = new ComponentBuilder().Build();
                });
            }
            else
            {
                await RespondAsync(successText, ephemeral: false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShopModule.HandleShopConfirmAsync] Error: {ex}");
            await RespondAsync("An error occurred while confirming your purchase.", ephemeral: true);
        }
    }

    [ComponentInteraction("shop_cancel:*")]
    public async Task HandleShopCancelAsync(ulong ownerId)
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
                await RespondAsync("You cannot cancel a purchase for another user.", ephemeral: true);
                return;
            }

            var guildId = Context.Guild.Id;
            var language = _localizationService.GetGuildLanguage(guildId);
            var cancelled = _localizationService.GetString("Shop.PurchaseCancelled", language);

            if (Context.Interaction is SocketMessageComponent component)
            {
                await component.UpdateAsync(msg =>
                {
                    msg.Content = cancelled;
                    msg.Components = new ComponentBuilder().Build();
                });
            }
            else
            {
                await RespondAsync(cancelled, ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShopModule.HandleShopCancelAsync] Error: {ex}");
            await RespondAsync("An error occurred while cancelling your purchase.", ephemeral: true);
        }
    }

    [ModalInteraction("shop_qty:*:*:*")]
    public async Task HandleShopQuantityModalAsync(int storeItemId, ulong ownerId, int locationId, ShopQuantityModal modal)
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

            if (Context.User.Id != ownerId)
            {
                var notYourShop = _localizationService.GetString("Shop.NotYourShop", language);
                await RespondAsync(notYourShop, ephemeral: true);
                return;
            }

            var player = await _dbContext.Players
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == ownerId);

            if (player is null || player.CurrentLocationId != locationId)
            {
                var wrongLocation = _localizationService.GetString("Shop.NotInThisLocation", language);
                await RespondAsync(wrongLocation, ephemeral: true);
                return;
            }

            if (!int.TryParse(modal.Quantity, out var quantity) || quantity <= 0)
            {
                var invalidQty = _localizationService.GetString("Shop.InvalidQuantity", language);
                await RespondAsync(invalidQty, ephemeral: true);
                return;
            }

            // Load the store item to show a confirmation
            var storeItem = await _dbContext.StoreTypeItems
                .Include(sti => sti.ItemType)
                .FirstOrDefaultAsync(sti => sti.Id == storeItemId && sti.Enabled);

            if (storeItem is null)
            {
                var unavailable = _localizationService.GetString("Shop.ItemUnavailable", language);
                await RespondAsync(unavailable, ephemeral: true);
                return;
            }

            var code = storeItem.ItemType.Code;
            var icon = storeItem.ItemType.IconCode;
            var nameKey = $"Item.{code}.Name";
            var localizedName = _localizationService.GetString(nameKey, language);
            var name = string.IsNullOrEmpty(localizedName) || localizedName == nameKey
                ? code
                : localizedName;

            var existingItem = player.Inventory.FirstOrDefault(ii => ii.ItemTypeId == storeItem.ItemTypeId);
            var currentQuantity = existingItem?.Quantity ?? 0;
            var currentMoney = player.Money;

            var totalPrice = storeItem.Price * quantity;

            var confirmCustomId = $"shop_confirm:{storeItem.Id}:{quantity}:{Context.User.Id}";
            var cancelCustomId = $"shop_cancel:{Context.User.Id}";

            var confirmTemplate = _localizationService.GetString("Shop.ConfirmPrompt", language);
            var confirmation = string.Format(confirmTemplate,
                quantity,
                icon,
                name,
                totalPrice,
                currentQuantity,
                currentMoney);

            var components = new ComponentBuilder()
                .WithButton("Confirm", confirmCustomId, ButtonStyle.Success)
                .WithButton("Cancel", cancelCustomId, ButtonStyle.Danger)
                .Build();

            // Ephemeral so only the buyer sees the confirmation and buttons
            await RespondAsync(confirmation, components: components, ephemeral: true);

            // Timeout: after 1 minute, clear the buttons if no action was taken
            var interaction = Context.Interaction;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    await interaction.ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Components = new ComponentBuilder().Build();
                    });
                }
                catch
                {
                    // Ignore timeout errors
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ShopModule.HandleShopQuantityModalAsync] Error: {ex}");
            await RespondAsync("An error occurred while processing your purchase.", ephemeral: true);
        }
    }

    public class ShopQuantityModal : IModal
    {
        public string Title => "Buy item";

        [InputLabel("Quantity")]
        [ModalTextInput("quantity")]
        public string Quantity { get; set; } = string.Empty;
    }
}
