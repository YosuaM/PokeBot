using System.Collections.Concurrent;
using System.Text;
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
            .Include(p => p.Inventory)
                .ThenInclude(ii => ii.ItemType)
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

        var ownerId = Context.User.Id;

        var ballCodes = new[] { "POKE_BALL", "GREAT_BALL", "ULTRA_BALL", "MASTER_BALL" };
        var inventoryBalls = player.Inventory
            .Where(ii => ballCodes.Contains(ii.ItemType.Code))
            .ToList();

        // Load item type metadata (icons) for all ball types so we always have emojis,
        // even if the player has 0 units of a given ball type.
        var ballItemTypes = await _dbContext.ItemTypes
            .Where(it => ballCodes.Contains(it.Code))
            .ToListAsync();

        // Load capture rates for this species rarity (if defined)
        var captureRatesByBall = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (species.PokemonRarityId.HasValue)
        {
            var rarityRates = await _dbContext.Set<PokemonRarityCatchRate>()
                .Where(cr => cr.PokemonRarityId == species.PokemonRarityId.Value && ballCodes.Contains(cr.BallCode))
                .ToListAsync();

            foreach (var cr in rarityRates)
            {
                captureRatesByBall[cr.BallCode] = cr.CatchRatePercent;
            }
        }

        var inventoryHeader = _localizationService.GetString("Catch.InventoryHeader", language);
        var ballLineTemplate = _localizationService.GetString("Catch.BallButtonLabel", language);
        var captureHeader = _localizationService.GetString("Catch.CaptureRatesHeader", language);
        var captureLineTemplate = _localizationService.GetString("Catch.CaptureRateLine", language);
        var actionPrompt = _localizationService.GetString("Catch.ActionPrompt", language);

        var fleeLabel = _localizationService.GetString("Catch.FleeLabel", language);

        var builder = new ComponentBuilder();

        var inventoryLines = new List<string>();
        var captureLines = new List<string>();

        foreach (var code in ballCodes)
        {
            var invItem = inventoryBalls.FirstOrDefault(ii => ii.ItemType.Code == code);
            var quantity = invItem?.Quantity ?? 0;

            var ballType = ballItemTypes.FirstOrDefault(it => it.Code == code);
            var itemIcon = ballType?.IconCode ?? string.Empty;

            // Build inventory line
            var nameKey = $"Item.{code}.Name";
            var localizedName = _localizationService.GetString(nameKey, language);
            var name = string.IsNullOrEmpty(localizedName) || localizedName == nameKey ? code : localizedName;
            var invLine = string.Format(ballLineTemplate, itemIcon, name, quantity);
            inventoryLines.Add(invLine);

            // Build capture rate line (if available)
            if (captureRatesByBall.TryGetValue(code, out var percent))
            {
                var captureLine = string.Format(captureLineTemplate, itemIcon, name, percent);
                captureLines.Add(captureLine);
            }

            if (!string.IsNullOrWhiteSpace(itemIcon) && Emote.TryParse(itemIcon, out var emote))
            {
                // Label must not be empty; use zero-width space so only the emoji is visible
                builder.WithButton(
                    label: "\u200B",
                    customId: $"catch_attempt:{species.Id}:{level}:{ownerId}:{code}",
                    style: ButtonStyle.Primary,
                    emote: emote,
                    disabled: quantity <= 0,
                    row: 0);
            }
            else
            {
                // Fallback text label when we don't have a valid emoji
                builder.WithButton(
                    label: name,
                    customId: $"catch_attempt:{species.Id}:{level}:{ownerId}:{code}",
                    style: ButtonStyle.Primary,
                    disabled: quantity <= 0,
                    row: 0);
            }
        }

        // Fight button (placeholder for future combat system) on second row
        var fightLabel = _localizationService.GetString("Catch.FightLabel", language);
        builder.WithButton(fightLabel, $"catch_fight:{species.Id}:{level}:{ownerId}", ButtonStyle.Secondary, row: 1);

        // Flee button on second row
        builder.WithButton(fleeLabel, $"catch_flee:{species.Id}:{level}:{ownerId}", ButtonStyle.Danger, row: 1);

        // Build description with Pokémon info + inventory + action prompt
        var descriptionBuilder = new StringBuilder();
        descriptionBuilder.AppendLine(appeared);
        descriptionBuilder.AppendLine();
        descriptionBuilder.AppendLine(inventoryHeader);
        foreach (var line in inventoryLines)
        {
            descriptionBuilder.AppendLine(line);
        }
        descriptionBuilder.AppendLine();

        if (captureLines.Count > 0)
        {
            descriptionBuilder.AppendLine(captureHeader);
            foreach (var line in captureLines)
            {
                descriptionBuilder.AppendLine(line);
            }
            descriptionBuilder.AppendLine();
        }

        descriptionBuilder.AppendLine(actionPrompt);

        var embedBuilder = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(descriptionBuilder.ToString())
            .WithColor(Color.DarkGreen);

        // If a sprite URL is defined for this species, show it as thumbnail
        if (!string.IsNullOrWhiteSpace(species.SpriteUrl))
        {
            embedBuilder.WithThumbnailUrl(species.SpriteUrl);
        }

        var embed = embedBuilder.Build();

        var components = builder.Build();

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

    [ComponentInteraction("catch_attempt:*:*:*:*")]
    public async Task HandleCatchAttemptAsync(int speciesId, int level, ulong ownerId, string ballCode)
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
            .Include(p => p.Inventory)
                .ThenInclude(ii => ii.ItemType)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == ownerId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var icon = string.IsNullOrWhiteSpace(species.IconCode) ? string.Empty : species.IconCode + " ";

        // Consume one ball of the chosen type
        var ballItem = player.Inventory.FirstOrDefault(ii => ii.ItemType.Code == ballCode);
        if (ballItem is null || ballItem.Quantity <= 0)
        {
            var noBallTemplate = _localizationService.GetString("Catch.NoBallsOfType", language);

            var ballType = await _dbContext.ItemTypes.FirstOrDefaultAsync(it => it.Code == ballCode);
            var itemIcon = ballType?.IconCode ?? string.Empty;
            var nameKey = $"Item.{ballCode}.Name";
            var localizedName = _localizationService.GetString(nameKey, language);
            var name = string.IsNullOrEmpty(localizedName) || localizedName == nameKey ? ballCode : localizedName;

            var message = string.Format(noBallTemplate, itemIcon, name);

            await RespondAsync(message, ephemeral: true);
            return;
        }

        ballItem.Quantity -= 1;
        await _dbContext.SaveChangesAsync();

        // Determine capture rate based on rarity and ball type
        var rarity = await _dbContext.Set<PokemonRarity>()
            .FirstOrDefaultAsync(r => r.Id == species.PokemonRarityId);

        int captureRate;
        if (rarity is not null)
        {
            var rate = await _dbContext.Set<PokemonRarityCatchRate>()
                .FirstOrDefaultAsync(cr => cr.PokemonRarityId == rarity.Id && cr.BallCode == ballCode);

            captureRate = rate?.CatchRatePercent ?? 50;
        }
        else
        {
            captureRate = 50;
        }
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

    [ComponentInteraction("catch_flee:*:*:*")]
    public async Task HandleCatchFleeAsync(int speciesId, int level, ulong ownerId)
    {
        if (Context.Guild is null)
        {
            var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
            await RespondAsync(msg, ephemeral: true);
            return;
        }

        if (Context.User.Id != ownerId)
        {
            await RespondAsync("You cannot flee from a Pokémon encounter for another user.", ephemeral: true);
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

        var icon = string.IsNullOrWhiteSpace(species.IconCode) ? string.Empty : species.IconCode + " ";

        var fledTemplate = _localizationService.GetString("Catch.FledByPlayer", language);
        var description = string.Format(fledTemplate, icon, species.Code, level);

        var resultTitle = _localizationService.GetString("Catch.ResultTitle", language);

        var embed = new EmbedBuilder()
            .WithTitle(resultTitle)
            .WithDescription(description)
            .WithColor(Color.DarkGrey)
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

    [ComponentInteraction("catch_fight:*:*:*")]
    public async Task HandleCatchFightAsync(int speciesId, int level, ulong ownerId)
    {
        if (Context.Guild is null)
        {
            var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
            await RespondAsync(msg, ephemeral: true);
            return;
        }

        if (Context.User.Id != ownerId)
        {
            await RespondAsync("You cannot fight a Pokémon for another user.", ephemeral: true);
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

        // Reward based on rarity
        var rarity = await _dbContext.Set<PokemonRarity>()
            .FirstOrDefaultAsync(r => r.Id == species.PokemonRarityId);

        int reward = 1; // fallback
        if (rarity is not null && rarity.MaxMoneyReward > 0)
        {
            reward = Random.Shared.Next(rarity.MinMoneyReward, rarity.MaxMoneyReward + 1);
        }

        player.Money += reward;
        await _dbContext.SaveChangesAsync();

        var icon = string.IsNullOrWhiteSpace(species.IconCode) ? string.Empty : species.IconCode + " ";

        var defeatedTemplate = _localizationService.GetString("Catch.Defeated", language);
        var description = string.Format(defeatedTemplate, icon, species.Code, level, reward);

        var resultTitle = _localizationService.GetString("Catch.ResultTitle", language);

        var embed = new EmbedBuilder()
            .WithTitle(resultTitle)
            .WithDescription(description)
            .WithColor(Color.DarkGreen)
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
