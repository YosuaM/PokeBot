using System.Collections.Concurrent;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class MovementModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    private static readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), CancellationTokenSource> _moveTimeouts = new();

    public MovementModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Regenerates stamina for the given player based on the time elapsed since LastTurnAtUtc
    /// and the per-guild StaminaPerHour configuration. Regeneration is applied in whole hours
    /// and never exceeds MaxStamina. Returns true if any change was applied.
    /// </summary>
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

        // Advance the last-turn marker by the number of full hours we just applied
        player.LastTurnAtUtc = last.AddHours(fullHours);

        return true;
    }

    [SlashCommand("move", "Move your trainer to a connected location, consuming stamina")]
    public async Task MoveAsync()
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
            .Include(p => p.CurrentLocation)
            .Include(p => p.GymBadges)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        // Apply time-based stamina regeneration before checking
        if (await RegenerateStaminaAsync(player))
        {
            await _dbContext.SaveChangesAsync();
        }

        if (player.CurrentStamina <= 0)
        {
            var noStamina = _localizationService.GetString("Move.NoStamina", language);
            await RespondAsync(noStamina, ephemeral: true);
            return;
        }

        var connections = await _dbContext.LocationConnections
            .Include(lc => lc.ToLocation)
            .AsNoTracking()
            .Where(lc => lc.FromLocationId == player.CurrentLocationId)
            .ToListAsync();

        // Filter connections by badges
        var reachable = new List<LocationConnection>();
        foreach (var conn in connections)
        {
            if (conn.RequiredGymId is null)
            {
                reachable.Add(conn);
                continue;
            }

            var hasBadge = player.GymBadges.Any(b => b.GymId == conn.RequiredGymId.Value);
            if (hasBadge)
            {
                reachable.Add(conn);
            }
        }

        if (reachable.Count == 0)
        {
            var noOptions = _localizationService.GetString("Move.NoAvailableDestinations", language);
            await RespondAsync(noOptions, ephemeral: true);
            return;
        }

        // Localized current location name
        string currentLocationName;
        if (player.CurrentLocation is not null && !string.IsNullOrWhiteSpace(player.CurrentLocation.Code))
        {
            var locKey = $"Locations.{player.CurrentLocation.Code}.Name";
            var localized = _localizationService.GetString(locKey, language);
            currentLocationName = localized == locKey ? player.CurrentLocation.Code : localized;
        }
        else
        {
            currentLocationName = player.CurrentLocationId.ToString();
        }

        var title = _localizationService.GetString("Move.Title", language);
        var descriptionTemplate = _localizationService.GetString("Move.Description", language);
        var description = string.Format(descriptionTemplate, currentLocationName, player.CurrentStamina);

        var components = new ComponentBuilder();
        foreach (var conn in reachable)
        {
            var toLocation = conn.ToLocation;
            var toKey = $"Locations.{toLocation.Code}.Name";
            var toLocalized = _localizationService.GetString(toKey, language);
            var toName = toLocalized == toKey ? toLocation.Code : toLocalized;

            components.WithButton(toName, $"move_select:{toLocation.Id}:{userId}", ButtonStyle.Primary);
        }

        await RespondAsync(embed: new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(Color.Blue)
                .Build(),
            components: components.Build(),
            ephemeral: true);

        // Schedule timeout to disable buttons after 1 minute
        var key = (GuildId: guildId, UserId: userId);
        if (_moveTimeouts.TryRemove(key, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _moveTimeouts[key] = cts;
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), token);
                if (token.IsCancellationRequested)
                    return;

                await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                {
                    var expiredTitle = _localizationService.GetString("Move.ExpiredTitle", language);
                    var expiredBody = _localizationService.GetString("Move.ExpiredBody", language);
                    msg.Embed = new EmbedBuilder()
                        .WithTitle(expiredTitle)
                        .WithDescription(expiredBody)
                        .WithColor(Color.DarkGrey)
                        .Build();
                    msg.Components = new ComponentBuilder().Build();
                });
            }
            catch (TaskCanceledException)
            {
            }
            catch
            {
            }
            finally
            {
                if (_moveTimeouts.TryRemove(key, out var toDispose))
                {
                    toDispose.Dispose();
                }
            }
        }, token);
    }

    [ComponentInteraction("move_select:*:*")]
    public async Task HandleMoveSelectAsync(int toLocationId, ulong ownerId)
    {
        if (Context.Guild is null)
        {
            var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
            await RespondAsync(msg, ephemeral: true);
            return;
        }

        if (Context.User.Id != ownerId)
        {
            await RespondAsync("You cannot move for another user.", ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var userId = Context.User.Id;
        var language = _localizationService.GetGuildLanguage(guildId);

        var player = await _dbContext.Players
            .Include(p => p.CurrentLocation)
            .Include(p => p.GymBadges)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        if (player.CurrentStamina <= 0)
        {
            var noStamina = _localizationService.GetString("Move.NoStamina", language);
            await RespondAsync(noStamina, ephemeral: true);
            return;
        }

        var connection = await _dbContext.LocationConnections
            .Include(lc => lc.ToLocation)
            .FirstOrDefaultAsync(lc => lc.FromLocationId == player.CurrentLocationId && lc.ToLocationId == toLocationId);

        if (connection is null)
        {
            var invalidDest = _localizationService.GetString("Move.InvalidDestination", language);
            await RespondAsync(invalidDest, ephemeral: true);
            return;
        }

        if (connection.RequiredGymId is not null)
        {
            var hasBadge = player.GymBadges.Any(b => b.GymId == connection.RequiredGymId.Value);
            if (!hasBadge)
            {
                var requiresBadge = _localizationService.GetString("Move.RequiresBadge", language);
                await RespondAsync(requiresBadge, ephemeral: true);
                return;
            }
        }

        var now = DateTime.UtcNow;

        player.CurrentLocationId = toLocationId;
        player.LastTurnAtUtc = now;
        if (player.CurrentStamina > 0)
        {
            player.CurrentStamina -= 1;
        }

        await _dbContext.SaveChangesAsync();

        // Cancel timeout so the message doesn't expire after moving
        var key = (GuildId: guildId, UserId: userId);
        if (_moveTimeouts.TryRemove(key, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        // Localized new location name
        var toLocation = connection.ToLocation;
        var toKey = $"Locations.{toLocation.Code}.Name";
        var toLocalized = _localizationService.GetString(toKey, language);
        var toName = toLocalized == toKey ? toLocation.Code : toLocalized;

        var staminaAfter = player.CurrentStamina;

        var movedTitle = _localizationService.GetString("Move.MovedTitle", language);
        var staminaLabel = _localizationService.GetString("Profile.StaminaLabel", language);

        var embed = new EmbedBuilder()
            .WithTitle(movedTitle)
            .WithColor(Color.Green)
            .AddField("Location", toName, inline: false)
            .AddField(staminaLabel, staminaAfter.ToString(), inline: false)
            .Build();

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = new ComponentBuilder().Build();
        });

        // Public announcement
        var announceTemplate = _localizationService.GetString("Move.Announcement", language);
        var announce = string.Format(announceTemplate, Context.User.Mention, toName);

        // After moving, roll for random events (item / surprise battle) and append the result
        var eventText = await TryTriggerMoveEventAsync(player, language);
        if (!string.IsNullOrWhiteSpace(eventText))
        {
            const string eventEmoji = ":bangbang:";

            // Prefix each event line with the emoji
            var lines = eventText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = $"\n{eventEmoji} {lines[i]}";
            }

            announce += "\n" + string.Join("\n", lines);
        }

        await FollowupAsync(announce, ephemeral: false);
    }

    private async Task<string?> TryTriggerMoveEventAsync(Player player, string language)
    {
        var settings = await _dbContext.GuildSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GuildId == player.GuildId);

        var itemChance = settings?.MoveItemEventChancePercent ?? 0;
        var battleChance = settings?.MoveBattleEventChancePercent ?? 0;

        if (itemChance <= 0 && battleChance <= 0)
        {
            return null;
        }

        var parts = new List<string>();

        // Roll independently for item and battle so both can trigger in the same move
        if (itemChance > 0)
        {
            var rollItem = Random.Shared.Next(0, 100);
            if (rollItem < itemChance)
            {
                var itemText = await HandleMoveItemEventAsync(player, language);
                if (!string.IsNullOrWhiteSpace(itemText))
                {
                    parts.Add(itemText);
                }
            }
        }

        if (battleChance > 0)
        {
            var rollBattle = Random.Shared.Next(0, 100);
            if (rollBattle < battleChance)
            {
                var battleText = await HandleMoveBattleEventAsync(player, language, settings);
                if (!string.IsNullOrWhiteSpace(battleText))
                {
                    parts.Add(battleText);
                }
            }
        }

        if (parts.Count == 0)
        {
            return null;
        }

        return string.Join("\n", parts);
    }

    private async Task<string?> HandleMoveItemEventAsync(Player player, string language)
    {
        var rewards = await _dbContext.MoveRandomItemRewards
            .Include(r => r.ItemType)
            .ToListAsync();

        if (rewards.Count == 0)
        {
            return null;
        }

        // Weighted random selection
        var totalWeight = rewards.Sum(r => Math.Max(1, r.Weight));
        var roll = Random.Shared.Next(1, totalWeight + 1);
        MoveRandomItemReward picked = rewards[^1];
        var cumulative = 0;
        foreach (var r in rewards)
        {
            cumulative += Math.Max(1, r.Weight);
            if (roll <= cumulative)
            {
                picked = r;
                break;
            }
        }

        var qty = Random.Shared.Next(Math.Max(1, picked.MinQuantity), Math.Max(picked.MinQuantity, picked.MaxQuantity) + 1);

        var invItem = await _dbContext.InventoryItems
            .FirstOrDefaultAsync(ii => ii.PlayerId == player.Id && ii.ItemTypeId == picked.ItemTypeId);

        if (invItem is null)
        {
            invItem = new InventoryItem
            {
                PlayerId = player.Id,
                ItemTypeId = picked.ItemTypeId,
                Quantity = qty
            };
            _dbContext.Add(invItem);
        }
        else
        {
            invItem.Quantity += qty;
        }

        await _dbContext.SaveChangesAsync();

        var icon = picked.ItemType.IconCode ?? string.Empty;
        var nameKey = $"Item.{picked.ItemType.Code}.Name";
        var localizedName = _localizationService.GetString(nameKey, language);
        var name = string.IsNullOrEmpty(localizedName) || localizedName == nameKey ? picked.ItemType.Code : localizedName;

        var bodyTemplate = _localizationService.GetString("Move.Event.ItemFound.Body", language);
        var body = string.Format(bodyTemplate, qty, icon, name);

        return body;
    }

    private async Task<string?> HandleMoveBattleEventAsync(Player player, string language, GuildSettings? settings)
    {
        // Placeholder battle: 50% chance to win
        var winRoll = Random.Shared.Next(0, 100);
        var win = winRoll < 50;

        if (!win)
        {
            var bodyLose = _localizationService.GetString("Move.Event.BattleLose.Body", language);
            return bodyLose;
        }

        var min = Math.Max(0, settings?.MoveBattleWinMinMoneyReward ?? 0);
        var max = Math.Max(min, settings?.MoveBattleWinMaxMoneyReward ?? min);

        var reward = Random.Shared.Next(min, max + 1);

        if (reward > 0)
        {
            player.Money += reward;
            await _dbContext.SaveChangesAsync();
        }

        var bodyTemplateWin = _localizationService.GetString("Move.Event.BattleWin.Body", language);
        var bodyWin = string.Format(bodyTemplateWin, reward);

        return bodyWin;
    }
}
