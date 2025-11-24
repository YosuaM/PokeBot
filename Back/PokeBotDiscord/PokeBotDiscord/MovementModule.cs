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

        await FollowupAsync(announce, ephemeral: false);
    }
}
