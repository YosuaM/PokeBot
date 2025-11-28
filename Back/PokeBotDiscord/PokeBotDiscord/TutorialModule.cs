using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class TutorialModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    public TutorialModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("tutorial", "Show your tutorial progress and claim rewards")] 
    public async Task TutorialAsync()
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
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var steps = await _dbContext.TutorialSteps
            .Include(ts => ts.Missions.OrderBy(m => m.Order))
            .ToListAsync();

        if (steps.Count == 0)
        {
            await RespondAsync("Tutorial is not configured yet.", ephemeral: true);
            return;
        }

        var missionProgress = await _dbContext.PlayerTutorialMissionProgresses
            .Where(p => p.PlayerId == player.Id)
            .ToListAsync();
        var missionProgressById = missionProgress.ToDictionary(p => p.TutorialMissionId, p => p);

        var stepProgress = await _dbContext.PlayerTutorialStepProgresses
            .Where(p => p.PlayerId == player.Id)
            .ToListAsync();
        var stepProgressById = stepProgress.ToDictionary(p => p.TutorialStepId, p => p);

        TutorialStep? currentStep = null;
        foreach (var step in steps.OrderBy(s => s.Order))
        {
            var allMissionsCompleted = step.Missions.All(m =>
                missionProgressById.TryGetValue(m.Id, out var mp) && mp.Completed);

            if (!allMissionsCompleted)
            {
                currentStep = step;
                break;
            }
        }

        currentStep ??= steps.OrderBy(s => s.Order).Last();

        var currentIndex = steps.OrderBy(s => s.Order).ToList().FindIndex(s => s.Id == currentStep.Id) + 1;
        var totalSteps = steps.Count;

        var titleTemplate = _localizationService.GetString("Tutorial.Title", language);
        var title = string.Format(titleTemplate, currentIndex, totalSteps);

        var intro = _localizationService.GetString(currentStep.IntroKey, language);

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(intro))
        {
            sb.AppendLine(intro);
            sb.AppendLine();
        }

        var pendingEmoji = ":white_large_square:";
        var doneEmoji = ":white_check_mark:";

        foreach (var mission in currentStep.Missions.OrderBy(m => m.Order))
        {
            var desc = _localizationService.GetString(mission.DescriptionKey, language);
            var completed = missionProgressById.TryGetValue(mission.Id, out var mp) && mp.Completed;
            var lineTemplateKey = completed
                ? "Tutorial.MissionLine.Completed"
                : "Tutorial.MissionLine.Pending";
            var lineTemplate = _localizationService.GetString(lineTemplateKey, language);
            var emoji = completed ? doneEmoji : pendingEmoji;
            var line = string.Format(lineTemplate, emoji, desc);
            sb.AppendLine(line);
        }

        sb.AppendLine();

        // Reward summary
        string? rewardText = null;
        if (currentStep.RewardMoney > 0 && currentStep.RewardItemTypeId is not null && currentStep.RewardItemQuantity > 0)
        {
            var itemType = await _dbContext.ItemTypes.FirstOrDefaultAsync(it => it.Id == currentStep.RewardItemTypeId.Value);
            string itemDisplay;
            if (itemType is null)
            {
                itemDisplay = "Item";
            }
            else
            {
                var itemName = _localizationService.GetString($"Item.{itemType.Code}.Name", language);
                var icon = itemType.IconCode ?? string.Empty;
                itemDisplay = string.IsNullOrWhiteSpace(icon) ? itemName : $"{icon} {itemName}";
            }
            var rewardTemplate = _localizationService.GetString("Tutorial.RewardLine.MoneyAndItem", language);
            rewardText = string.Format(rewardTemplate, currentStep.RewardMoney, currentStep.RewardItemQuantity, itemDisplay);
        }
        else if (currentStep.RewardMoney > 0)
        {
            var rewardTemplate = _localizationService.GetString("Tutorial.RewardLine.MoneyOnly", language);
            rewardText = string.Format(rewardTemplate, currentStep.RewardMoney);
        }
        else if (currentStep.RewardItemTypeId is not null && currentStep.RewardItemQuantity > 0)
        {
            var itemType = await _dbContext.ItemTypes.FirstOrDefaultAsync(it => it.Id == currentStep.RewardItemTypeId.Value);
            string itemDisplay;
            if (itemType is null)
            {
                itemDisplay = "Item";
            }
            else
            {
                var itemName = _localizationService.GetString($"Item.{itemType.Code}.Name", language);
                var icon = itemType.IconCode ?? string.Empty;
                itemDisplay = string.IsNullOrWhiteSpace(icon) ? itemName : $"{icon} {itemName}";
            }
            var rewardTemplate = _localizationService.GetString("Tutorial.RewardLine.ItemOnly", language);
            rewardText = string.Format(rewardTemplate, currentStep.RewardItemQuantity, itemDisplay);
        }

        if (!string.IsNullOrWhiteSpace(rewardText))
        {
            sb.AppendLine(rewardText);
        }

        var allCompletedForStep = currentStep.Missions.All(m =>
            missionProgressById.TryGetValue(m.Id, out var mp) && mp.Completed);

        stepProgressById.TryGetValue(currentStep.Id, out var currentStepProgress);
        var rewardAlreadyClaimed = currentStepProgress?.RewardClaimed == true;

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(sb.ToString())
            .WithColor(Color.Gold)
            .Build();

        var components = new ComponentBuilder();
        var canClaim = allCompletedForStep && !rewardAlreadyClaimed;

        var claimLabel = _localizationService.GetString("Tutorial.Button.Claim", language);
        components.WithButton(claimLabel, $"tutorial_claim:{currentStep.Id}", ButtonStyle.Success, disabled: !canClaim);

        await RespondAsync(embed: embed, components: components.Build(), ephemeral: true);
    }

    [ComponentInteraction("tutorial_claim:*")]
    public async Task HandleTutorialClaimAsync(int stepId)
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
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var step = await _dbContext.TutorialSteps
            .Include(ts => ts.Missions)
            .FirstOrDefaultAsync(ts => ts.Id == stepId);

        if (step is null)
        {
            await RespondAsync("Tutorial step not found.", ephemeral: true);
            return;
        }

        var missionIds = step.Missions.Select(m => m.Id).ToList();
        var missionProgress = await _dbContext.PlayerTutorialMissionProgresses
            .Where(p => p.PlayerId == player.Id && missionIds.Contains(p.TutorialMissionId))
            .ToListAsync();

        var allCompleted = step.Missions.All(m =>
            missionProgress.Any(mp => mp.TutorialMissionId == m.Id && mp.Completed));

        if (!allCompleted)
        {
            await RespondAsync("You have not completed all missions for this tutorial step yet.", ephemeral: true);
            return;
        }

        var stepProgress = await _dbContext.PlayerTutorialStepProgresses
            .FirstOrDefaultAsync(p => p.PlayerId == player.Id && p.TutorialStepId == step.Id);

        if (stepProgress is not null && stepProgress.RewardClaimed)
        {
            await RespondAsync("You have already claimed this reward.", ephemeral: true);
            return;
        }

        if (stepProgress is null)
        {
            stepProgress = new PlayerTutorialStepProgress
            {
                PlayerId = player.Id,
                TutorialStepId = step.Id,
                RewardClaimed = false
            };
            _dbContext.Add(stepProgress);
        }

        // Apply rewards
        if (step.RewardMoney > 0)
        {
            player.Money += step.RewardMoney;
        }

        if (step.RewardItemTypeId is not null && step.RewardItemQuantity > 0)
        {
            var invItem = await _dbContext.InventoryItems
                .FirstOrDefaultAsync(ii => ii.PlayerId == player.Id && ii.ItemTypeId == step.RewardItemTypeId.Value);

            if (invItem is null)
            {
                invItem = new InventoryItem
                {
                    PlayerId = player.Id,
                    ItemTypeId = step.RewardItemTypeId.Value,
                    Quantity = step.RewardItemQuantity
                };
                _dbContext.Add(invItem);
            }
            else
            {
                invItem.Quantity += step.RewardItemQuantity;
            }
        }

        stepProgress.RewardClaimed = true;
        stepProgress.RewardClaimedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        // Build claimed message including rewards
        string claimedMsg;
        string? rewardDetails = null;

        if (step.RewardMoney > 0 && step.RewardItemTypeId is not null && step.RewardItemQuantity > 0)
        {
            var itemType = await _dbContext.ItemTypes.FirstOrDefaultAsync(it => it.Id == step.RewardItemTypeId.Value);
            string itemDisplay;
            if (itemType is null)
            {
                itemDisplay = "Item";
            }
            else
            {
                var itemName = _localizationService.GetString($"Item.{itemType.Code}.Name", language);
                var icon = itemType.IconCode ?? string.Empty;
                itemDisplay = string.IsNullOrWhiteSpace(icon) ? itemName : $"{icon} {itemName}";
            }

            var rewardTemplate = _localizationService.GetString("Tutorial.RewardLine.MoneyAndItem", language);
            rewardDetails = string.Format(rewardTemplate, step.RewardMoney, step.RewardItemQuantity, itemDisplay);
        }
        else if (step.RewardMoney > 0)
        {
            var rewardTemplate = _localizationService.GetString("Tutorial.RewardLine.MoneyOnly", language);
            rewardDetails = string.Format(rewardTemplate, step.RewardMoney);
        }
        else if (step.RewardItemTypeId is not null && step.RewardItemQuantity > 0)
        {
            var itemType = await _dbContext.ItemTypes.FirstOrDefaultAsync(it => it.Id == step.RewardItemTypeId.Value);
            string itemDisplay;
            if (itemType is null)
            {
                itemDisplay = "Item";
            }
            else
            {
                var itemName = _localizationService.GetString($"Item.{itemType.Code}.Name", language);
                var icon = itemType.IconCode ?? string.Empty;
                itemDisplay = string.IsNullOrWhiteSpace(icon) ? itemName : $"{icon} {itemName}";
            }

            var rewardTemplate = _localizationService.GetString("Tutorial.RewardLine.ItemOnly", language);
            rewardDetails = string.Format(rewardTemplate, step.RewardItemQuantity, itemDisplay);
        }

        var claimedBase = _localizationService.GetString("Tutorial.RewardClaimed", language);
        if (!string.IsNullOrWhiteSpace(rewardDetails))
        {
            claimedMsg = $"{claimedBase}\n{rewardDetails}";
        }
        else
        {
            claimedMsg = claimedBase;
        }

        // Public announcement: player completed tutorial step X/Y
        var steps = await _dbContext.TutorialSteps.AsNoTracking().OrderBy(s => s.Order).ToListAsync();
        var totalSteps = steps.Count;
        var ordered = steps.ToList();
        var currentIndex = ordered.FindIndex(s => s.Id == step.Id) + 1;

        var announceTemplate = _localizationService.GetString("Tutorial.StepCompleted.Announcement", language);
        var announceText = string.Format(announceTemplate, Context.User.Mention, currentIndex, totalSteps);

        var titleTemplate = _localizationService.GetString("Tutorial.Title", language);
        var title = string.Format(titleTemplate, currentIndex, totalSteps);

        var announceEmbed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(announceText)
            .WithColor(Color.Gold)
            .Build();

        await Context.Channel.SendMessageAsync(embed: announceEmbed);

        if (Context.Interaction is SocketMessageComponent component)
        {
            // Disable the claim button in the original message
            var claimLabel = _localizationService.GetString("Tutorial.Button.Claim", language);
            await component.UpdateAsync(msg =>
            {
                msg.Components = new ComponentBuilder()
                    .WithButton(claimLabel, $"tutorial_claim:{step.Id}", ButtonStyle.Success, disabled: true)
                    .Build();
            });

            await FollowupAsync(claimedMsg, ephemeral: true);
        }
        else
        {
            await RespondAsync(claimedMsg, ephemeral: true);
        }
    }
}
