using System.Collections.Concurrent;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class AdventureModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    private static readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), CancellationTokenSource> _startTimeouts = new();

    public AdventureModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("start", "Start your Pokémon adventure on this server")] 
    public async Task StartAsync()
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

        // Check if player already exists for this guild
        var existingPlayer = await _dbContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (existingPlayer is not null)
        {
            var alreadyStarted = _localizationService.GetString("Adventure.AlreadyStarted", language);
            await RespondAsync(alreadyStarted, ephemeral: true);
            return;
        }

        // Build intro embed and starter selection buttons from configured starter species
        var introTitle = _localizationService.GetString("Adventure.Start.IntroTitle", language);
        var introBody = _localizationService.GetString("Adventure.Start.IntroBody", language);
        var confirmLabel = _localizationService.GetString("Adventure.Start.ConfirmLabel", language);

        var starters = await _dbContext.PokemonSpecies
            .Where(s => s.IsStarter && s.Enabled)
            .OrderBy(s => s.Id)
            .ToListAsync();

        if (starters.Count == 0)
        {
            await RespondAsync("No starter species are configured in the database.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle(introTitle)
            .WithDescription(introBody)
            .WithColor(Color.Green)
            .Build();

        var components = new ComponentBuilder();

        foreach (var starter in starters)
        {
            components.WithButton(starter.Code, $"starter_select:{starter.Code}:{userId}", ButtonStyle.Secondary);
        }

        components.WithButton(confirmLabel, $"starter_confirm::{userId}", ButtonStyle.Success, disabled: true);

        // Initial selection message is ephemeral (only the user sees the starter selection flow)
        await RespondAsync(embed: embed, components: components.Build(), ephemeral: true);

        // Schedule timeout to disable buttons after 1 minute
        var key = (GuildId: guildId, UserId: userId);
        if (_startTimeouts.TryRemove(key, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _startTimeouts[key] = cts;
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

                await Context.Interaction.ModifyOriginalResponseAsync(msg =>
                {
                    var expiredTitle = _localizationService.GetString("Adventure.Start.ExpiredTitle", language);
                    var expiredBody = _localizationService.GetString("Adventure.Start.ExpiredBody", language);
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
                // Timeout was cancelled because the starter was confirmed
            }
            catch
            {
                // Ignore if the original response no longer exists or was already modified
            }
            finally
            {
                if (_startTimeouts.TryRemove(key, out var toDispose))
                {
                    toDispose.Dispose();
                }
            }
        }, token);
    }

    [ComponentInteraction("starter_select:*:*")]
    public async Task HandleStarterSelectAsync(string starterCode, ulong ownerId)
    {
        try
        {
            // Only the user who initiated /start can interact
            if (Context.User.Id != ownerId)
            {
                await RespondAsync("You cannot select a starter for another user.", ephemeral: true);
                return;
            }

            var guildId = Context.Guild?.Id ?? 0;
            var language = guildId == 0 ? "en" : _localizationService.GetGuildLanguage(guildId);

            var confirmLabel = _localizationService.GetString("Adventure.Start.ConfirmLabel", language);

            var starters = await _dbContext.PokemonSpecies
                .Where(s => s.IsStarter && s.Enabled)
                .OrderBy(s => s.Id)
                .ToListAsync();

            if (starters.All(s => !string.Equals(s.Code, starterCode, StringComparison.OrdinalIgnoreCase)))
            {
                await RespondAsync("Selected starter is not valid.", ephemeral: true);
                return;
            }

            var components = new ComponentBuilder();

            foreach (var starter in starters)
            {
                var style = string.Equals(starter.Code, starterCode, StringComparison.OrdinalIgnoreCase)
                    ? ButtonStyle.Primary
                    : ButtonStyle.Secondary;

                components.WithButton(starter.Code, $"starter_select:{starter.Code}:{ownerId}", style);
            }

            components.WithButton(confirmLabel, $"starter_confirm:{starterCode}:{ownerId}", ButtonStyle.Success, disabled: false);

            var component = (SocketMessageComponent)Context.Interaction;
            await component.UpdateAsync(msg =>
            {
                msg.Components = components.Build();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleStarterSelectAsync: {ex}");

            if (!Context.Interaction.HasResponded)
            {
                await RespondAsync("An error occurred while processing your selection.", ephemeral: true);
            }
        }
    }

    [ComponentInteraction("starter_confirm:*:*")]
    public async Task HandleStarterConfirmAsync(string starterCode, ulong ownerId)
    {
        if (Context.Guild is null)
        {
            await RespondAsync("This command can only be used inside a server.", ephemeral: true);
            return;
        }

        if (Context.User.Id != ownerId)
        {
            await RespondAsync("You cannot confirm a starter for another user.", ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var userId = Context.User.Id;
        var language = _localizationService.GetGuildLanguage(guildId);

        // Ensure species exists and is marked as starter
        var species = await _dbContext.PokemonSpecies
            .FirstOrDefaultAsync(s => s.Code == starterCode && s.IsStarter && s.Enabled);

        if (species is null)
        {
            var missingSpecies = _localizationService.GetString("Adventure.SpeciesNotFound", language);
            await RespondAsync(missingSpecies, ephemeral: true);
            return;
        }

        // Double-check player does not already exist
        var existingPlayer = await _dbContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (existingPlayer is not null)
        {
            var alreadyStarted = _localizationService.GetString("Adventure.AlreadyStarted", language);
            await RespondAsync(alreadyStarted, ephemeral: true);
            return;
        }

        var now = DateTime.UtcNow;

        // Resolve initial location (PalletTown) from database instead of hardcoding Id = 1
        var initialLocation = await _dbContext.Locations
            .Where(l => l.Code == "PalletTown" && l.Enabled && !l.Hidden)
            .FirstOrDefaultAsync();

        if (initialLocation is null)
        {
            var locationMissing = _localizationService.GetString("Adventure.Start.InitialLocationNotFound", language);
            await RespondAsync(locationMissing, ephemeral: true);
            return;
        }

        var player = new Player
        {
            GuildId = guildId,
            DiscordUserId = userId,
            CurrentLocationId = initialLocation.Id,
            Money = 0,
            LastTurnAtUtc = now,
            CurrentStamina = 5,
            MaxStamina = 5,
        };

        var starterInstance = new PokemonInstance
        {
            Owner = player,
            PokemonSpeciesId = species.Id,
            Level = 1,
            InParty = true
        };

        player.Party.Add(starterInstance);

        _dbContext.Players.Add(player);
        await _dbContext.SaveChangesAsync();

        var title = _localizationService.GetString("Adventure.Start.Title", language);
        var trainerLabel = _localizationService.GetString("Adventure.Start.TrainerLabel", language);
        var serverLabel = _localizationService.GetString("Adventure.Start.ServerLabel", language);
        var locationLabel = _localizationService.GetString("Adventure.Start.LocationLabel", language);
        var starterLabel = _localizationService.GetString("Adventure.Start.StarterLabel", language);
        var initialLocationName = _localizationService.GetString("Locations.PalletTown.Name", language);

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Color.Green)
            .AddField(trainerLabel, Context.User.Mention, inline: false)
            .AddField(serverLabel, Context.Guild.Name, inline: false)
            .AddField(locationLabel, initialLocationName, inline: true)
            .AddField(starterLabel, starterCode, inline: true)
            .Build();

        // Disable buttons after confirmation and show final embed (still ephemeral)
        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = new ComponentBuilder().Build();
        });

        // Cancel the timeout so the selection message does not expire after confirmation
        var key = (GuildId: guildId, UserId: userId);
        if (_startTimeouts.TryRemove(key, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        // Public announcement so everyone sees that the player has started their adventure
        var announcementTemplate = _localizationService.GetString("Adventure.Start.Announcement", language);
        var announcement = string.Format(announcementTemplate, Context.User.Mention, initialLocationName, starterCode);

        await FollowupAsync(announcement, ephemeral: false);
    }
}
