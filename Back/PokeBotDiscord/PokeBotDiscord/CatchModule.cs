using System.Collections.Concurrent;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class CatchModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    private static readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), CancellationTokenSource> _catchTimeouts = new();

    public CatchModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("catch", "Try to encounter a wild Pokémon in your current location")] 
    public async Task CatchAsync()
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
                .ThenInclude(l => l.LocationType)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var location = player.CurrentLocation;
        if (location is null || !location.Enabled || location.Hidden)
        {
            var cannotEncounter = _localizationService.GetString("Catch.NoEncountersHere", language);
            await RespondAsync(cannotEncounter, ephemeral: true);
            return;
        }

        var locationType = location.LocationType;
        if (locationType is null || !locationType.HasWildEncounters)
        {
            var cannotEncounter = _localizationService.GetString("Catch.NoEncountersHere", language);
            await RespondAsync(cannotEncounter, ephemeral: true);
            return;
        }

        // Check stamina before allowing the catch attempt
        if (player.CurrentStamina <= 0)
        {
            var noStamina = _localizationService.GetString("Move.NoStamina", language);
            await RespondAsync(noStamina, ephemeral: true);
            return;
        }

        // Load encounter table for this location
        var encounters = await _dbContext.Set<PokemonEncounter>()
            .Include(e => e.Species)
            .Where(e => e.LocationId == location.Id && e.Species.Enabled)
            .ToListAsync();

        if (encounters.Count == 0)
        {
            var noneHere = _localizationService.GetString("Catch.NoEncountersDefined", language);
            await RespondAsync(noneHere, ephemeral: true);
            return;
        }

        // Consume 1 stamina for this catch action now that we know an encounter table exists
        if (player.CurrentStamina > 0)
        {
            player.CurrentStamina -= 1;
            await _dbContext.SaveChangesAsync();
        }

        // Weighted random selection by Weight
        var totalWeight = encounters.Sum(e => Math.Max(1, e.Weight));
        var roll = Random.Shared.Next(1, totalWeight + 1);
        PokemonEncounter? picked = null;
        var cumulative = 0;
        foreach (var e in encounters)
        {
            cumulative += Math.Max(1, e.Weight);
            if (roll <= cumulative)
            {
                picked = e;
                break;
            }
        }

        picked ??= encounters.Last();

        // Determine encounter level within [MinLevel, MaxLevel]
        var minLevel = Math.Max(1, picked.MinLevel);
        var maxLevel = Math.Max(minLevel, picked.MaxLevel);
        var level = Random.Shared.Next(minLevel, maxLevel + 1);

        var species = picked.Species;
        var icon = string.IsNullOrWhiteSpace(species.IconCode) ? string.Empty : species.IconCode + " ";

        // Register species in Pokédex if this is the first time the player sees it
        var existingDexEntry = await _dbContext.Set<PokemonInstance>()
            .FirstOrDefaultAsync(pi => pi.PlayerId == player.Id && pi.PokemonSpeciesId == species.Id);

        if (existingDexEntry is null)
        {
            var dexEntry = new PokemonInstance
            {
                PlayerId = player.Id,
                PokemonSpeciesId = species.Id,
                Level = 0,
                InParty = false
            };

            _dbContext.Add(dexEntry);
            await _dbContext.SaveChangesAsync();
        }

        var title = _localizationService.GetString("Catch.Title", language);
        var appearedTemplate = _localizationService.GetString("Catch.Appeared", language);
        var appeared = string.Format(appearedTemplate, icon, species.Code, level);

        var catchLabel = _localizationService.GetString("Catch.ButtonLabel", language);

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(appeared)
            .WithColor(Color.DarkGreen)
            .Build();

        var ownerId = Context.User.Id;

        var components = new ComponentBuilder()
            .WithButton(catchLabel, $"catch_attempt:{species.Id}:{level}:{ownerId}", ButtonStyle.Success)
            .Build();

        await RespondAsync(embed: embed, components: components, ephemeral: false);

        // Schedule timeout to disable the catch button after 1 minute
        var key = (GuildId: guildId, UserId: ownerId);
        if (_catchTimeouts.TryRemove(key, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _catchTimeouts[key] = cts;
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                var expiredTitle = _localizationService.GetString("Catch.ExpiredTitle", language);
                var expiredTemplate = _localizationService.GetString("Catch.ExpiredBody", language);
                var expiredBody = string.Format(expiredTemplate, icon, species.Code, level);

                await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                {
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
                if (_catchTimeouts.TryRemove(key, out var toDispose))
                {
                    toDispose.Dispose();
                }
            }
        }, token);
    }

    [ComponentInteraction("catch_attempt:*:*:*")]
    public async Task HandleCatchAttemptAsync(int speciesId, int level, ulong ownerId)
    {
        if (Context.Guild is null)
        {
            var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
            await RespondAsync(msg, ephemeral: true);
            return;
        }

        if (Context.User.Id != ownerId)
        {
            await RespondAsync("You cannot attempt to catch a Pokémon for another user.", ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var language = _localizationService.GetGuildLanguage(guildId);

        var species = await _dbContext.PokemonSpecies
            .FirstOrDefaultAsync(s => s.Id == speciesId && s.Enabled);

        if (species is null)
        {
            var noneHere = _localizationService.GetString("Catch.NoEncountersDefined", language);
            await RespondAsync(noneHere, ephemeral: true);
            return;
        }

        var player = await _dbContext.Players
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == ownerId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var icon = string.IsNullOrWhiteSpace(species.IconCode) ? string.Empty : species.IconCode + " ";
        var captureRate = Math.Clamp(species.CaptureRate, 0, 100);
        var roll = Random.Shared.Next(0, 100);
        var success = roll < captureRate;

        string description;
        if (success)
        {
            // Increment Pokédex/ownership level for this species using the captured level
            var instance = await _dbContext.Set<PokemonInstance>()
                .FirstOrDefaultAsync(pi => pi.PlayerId == player.Id && pi.PokemonSpeciesId == species.Id);

            if (instance is null)
            {
                instance = new PokemonInstance
                {
                    PlayerId = player.Id,
                    PokemonSpeciesId = species.Id,
                    Level = level,
                    InParty = false
                };

                _dbContext.Add(instance);
            }
            else
            {
                instance.Level += level;
            }

            await _dbContext.SaveChangesAsync();

            var caughtTemplate = _localizationService.GetString("Catch.Caught", language);
            description = string.Format(caughtTemplate, icon, species.Code, level);
        }
        else
        {
            var fledTemplate = _localizationService.GetString("Catch.Fled", language);
            description = string.Format(fledTemplate, icon, species.Code, level);
        }

        var resultTitle = _localizationService.GetString("Catch.ResultTitle", language);

        var embed = new EmbedBuilder()
            .WithTitle(resultTitle)
            .WithDescription(description)
            .WithColor(success ? Color.Gold : Color.DarkGrey)
            .Build();

        // Cancel timeout and clear buttons
        var key = (GuildId: guildId, UserId: ownerId);
        if (_catchTimeouts.TryRemove(key, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        if (Context.Interaction is SocketMessageComponent component)
        {
            await component.UpdateAsync(msg =>
            {
                msg.Embed = embed;
                msg.Components = new ComponentBuilder().Build();
            });
        }
        else
        {
            await RespondAsync(embed: embed, ephemeral: false);
        }
    }
}
