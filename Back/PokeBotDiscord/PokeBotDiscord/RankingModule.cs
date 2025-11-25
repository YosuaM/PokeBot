using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

[Group("ranking", "Show server rankings")] 
public class RankingModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    public RankingModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("badges", "Show the top trainers by number of gym badges on this server")] 
    public async Task BadgesAsync()
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

            // Group by player and count badges in this guild
            var badgeRanksQuery = _dbContext.PlayerGymBadges
                .Where(b => b.Player.GuildId == guildId)
                .GroupBy(b => new { b.PlayerId, b.Player.DiscordUserId })
                .Select(g => new
                {
                    g.Key.PlayerId,
                    g.Key.DiscordUserId,
                    Count = g.Count()
                });

            // Order in memory to avoid ordering by ulong in SQLite
            var badgeRanks = (await badgeRanksQuery.ToListAsync())
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.DiscordUserId)
                .Take(10)
                .ToList();

            var title = _localizationService.GetString("Ranking.Badges.Title", language);
            var emptyText = _localizationService.GetString("Ranking.Badges.Empty", language);
            var rowTemplate = _localizationService.GetString("Ranking.Badges.Row", language);

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(Color.Gold);

            if (badgeRanks.Count == 0)
            {
                embed.WithDescription(emptyText);
            }
            else
            {
                var lines = new List<string>();
                var rank = 1;
                foreach (var entry in badgeRanks)
                {
                    var guildUser = Context.Guild.GetUser(entry.DiscordUserId);
                    var name = guildUser?.Username ?? $"<@{entry.DiscordUserId}>";
                    var line = string.Format(rowTemplate, rank, name, entry.Count);
                    lines.Add(line);
                    rank++;
                }

                embed.WithDescription(string.Join("\n", lines));
            }

            await RespondAsync(embed: embed.Build(), ephemeral: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ranking.BadgesAsync] {ex}");
            await RespondAsync("An internal error occurred while building the badges ranking.", ephemeral: true);
        }
    }

    [SlashCommand("pokedex", "Show the top trainers by number of Pokedex entries on this server")] 
    public async Task PokedexAsync()
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

            // Group by player and count distinct species in their Pokedex for this guild
            var pokedexRanksQuery = _dbContext.Set<PokemonInstance>()
                .Where(pi => pi.Owner.GuildId == guildId)
                .GroupBy(pi => new { pi.PlayerId, pi.Owner.DiscordUserId })
                .Select(g => new
                {
                    g.Key.PlayerId,
                    g.Key.DiscordUserId,
                    Count = g.Select(pi => pi.PokemonSpeciesId).Distinct().Count()
                });

            // Order in memory to avoid ordering by ulong in SQLite
            var pokedexRanks = (await pokedexRanksQuery.ToListAsync())
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.DiscordUserId)
                .Take(10)
                .ToList();

            // Total enabled species for this server (used as denominator in the ranking)
            var totalSpecies = await _dbContext.PokemonSpecies
                .CountAsync(s => s.Enabled);

            var title = _localizationService.GetString("Ranking.Pokedex.Title", language);
            var emptyText = _localizationService.GetString("Ranking.Pokedex.Empty", language);
            var rowTemplate = _localizationService.GetString("Ranking.Pokedex.Row", language);

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(Color.Blue);

            if (pokedexRanks.Count == 0)
            {
                embed.WithDescription(emptyText);
            }
            else
            {
                var lines = new List<string>();
                var rank = 1;
                foreach (var entry in pokedexRanks)
                {
                    var guildUser = Context.Guild.GetUser(entry.DiscordUserId);
                    var name = guildUser?.Username ?? $"<@{entry.DiscordUserId}>";
                    // Ranking.Pokedex.Row: "#{0} - {1} - {2}/{3} species discovered"
                    var line = string.Format(rowTemplate, rank, name, entry.Count, totalSpecies);
                    lines.Add(line);
                    rank++;
                }

                embed.WithDescription(string.Join("\n", lines));
            }

            await RespondAsync(embed: embed.Build(), ephemeral: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ranking.PokedexAsync] {ex}");
            await RespondAsync("An internal error occurred while building the Pokédex ranking.", ephemeral: true);
        }
    }
}
