using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;

namespace PokeBotDiscord.Services;

public class TutorialService : ITutorialService
{
    private readonly PokeBotDbContext _dbContext;

    public TutorialService(PokeBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CompleteMissionsAsync(ulong guildId, ulong discordUserId, string conditionCode)
    {
        var player = await _dbContext.Players
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == discordUserId);

        if (player is null)
        {
            return;
        }

        var missions = await _dbContext.TutorialMissions
            .Where(m => m.ConditionCode == conditionCode)
            .ToListAsync();

        if (missions.Count == 0)
        {
            return;
        }

        var missionIds = missions.Select(m => m.Id).ToList();

        var existingProgress = await _dbContext.PlayerTutorialMissionProgresses
            .Where(p => p.PlayerId == player.Id && missionIds.Contains(p.TutorialMissionId))
            .ToListAsync();

        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var mission in missions)
        {
            var progress = existingProgress.FirstOrDefault(p => p.TutorialMissionId == mission.Id);
            if (progress is null)
            {
                progress = new PlayerTutorialMissionProgress
                {
                    PlayerId = player.Id,
                    TutorialMissionId = mission.Id,
                    Completed = true,
                    CompletedAtUtc = now
                };
                _dbContext.PlayerTutorialMissionProgresses.Add(progress);
                changed = true;
            }
            else if (!progress.Completed)
            {
                progress.Completed = true;
                progress.CompletedAtUtc = now;
                changed = true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
