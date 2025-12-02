using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class GymModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    public GymModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("gym", "Show information about the gym in your current location")] 
    public async Task GymAsync()
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
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var gym = await _dbContext.Gyms
            .Include(g => g.Trainers)
            .FirstOrDefaultAsync(g => g.LocationId == player.CurrentLocationId);

        if (gym is null)
        {
            var noGym = _localizationService.GetString("Gym.NoGymHere", language);
            await RespondAsync(noGym, ephemeral: true);
            return;
        }

        if (!gym.IsOpen)
        {
            var closed = _localizationService.GetString("Gym.Closed", language);
            await RespondAsync(closed, ephemeral: true);
            return;
        }

        // Localized location name
        string locationName;
        if (player.CurrentLocation is not null && !string.IsNullOrWhiteSpace(player.CurrentLocation.Code))
        {
            var locKey = $"Locations.{player.CurrentLocation.Code}.Name";
            var localized = _localizationService.GetString(locKey, language);
            locationName = localized == locKey ? player.CurrentLocation.Code : localized;
        }
        else
        {
            locationName = player.CurrentLocationId.ToString();
        }

        var titleTemplate = _localizationService.GetString("Gym.Title", language);
        var title = string.Format(titleTemplate, locationName);

        var header = _localizationService.GetString("Gym.TrainerListHeader", language);
        var lineTemplate = _localizationService.GetString("Gym.TrainerLine", language);
        var leaderTemplate = _localizationService.GetString("Gym.LeaderLine", language);
        var defeatedPrefix = _localizationService.GetString("Gym.TrainerDefeatedPrefix", language);

        var trainers = gym.Trainers
            .OrderBy(t => t.Order)
            .ThenBy(t => t.Name)
            .ToList();

        // Load progress for this player in this gym
        var trainerIds = trainers.Select(t => t.Id).ToList();
        var progress = await _dbContext.PlayerGymTrainerProgresses
            .Where(p => p.PlayerId == player.Id && trainerIds.Contains(p.GymTrainerId) && p.Defeated)
            .ToListAsync();
        var defeatedIds = new HashSet<int>(progress.Select(p => p.GymTrainerId));

        var lines = new List<string>();
        lines.Add(header);

        if (trainers.Count == 0)
        {
            lines.Add("-");
        }
        else
        {
            var index = 1;
            foreach (var trainer in trainers)
            {
                var isLeader = index == trainers.Count;
                string line;

                if (isLeader)
                {
                    var badge = gym.BadgeCode ?? string.Empty;
                    line = string.Format(leaderTemplate, index, trainer.Name, badge);
                }
                else
                {
                    line = string.Format(lineTemplate, index, trainer.Name);
                }

                if (defeatedIds.Contains(trainer.Id))
                {
                    line = defeatedPrefix + line;
                }
                lines.Add(line);
                index++;
            }
        }

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(string.Join("\n", lines))
            .WithColor(Color.DarkGreen)
            .Build();

        var components = new ComponentBuilder();

        // Determine next trainer to fight (first non-defeated in order)
        var nextTrainer = trainers.FirstOrDefault(t => !defeatedIds.Contains(t.Id));
        if (nextTrainer is not null)
        {
            var buttonLabel = _localizationService.GetString("Gym.NextFightButton", language);
            components.WithButton(buttonLabel, $"gym_fight:{gym.Id}:{nextTrainer.Id}:{userId}", ButtonStyle.Danger);
        }
        else if (trainers.Count > 0)
        {
            // All trainers defeated for this player. If they don't have the badge yet, offer claim button.
            var alreadyHasBadge = await _dbContext.PlayerGymBadges
                .AnyAsync(b => b.PlayerId == player.Id && b.GymId == gym.Id);

            if (!alreadyHasBadge)
            {
                var label = _localizationService.GetString("Gym.BadgeClaimButton", language);
                components.WithButton(label, $"gym_badge:{gym.Id}:{userId}", ButtonStyle.Success);
            }
        }

        // Gym info (trainers, leader, badge) is public so everyone sees the available challenges
        await RespondAsync(embed: embed, components: components.Build(), ephemeral: false);
    }

    [ComponentInteraction("gym_fight:*:*:*")]
    public async Task HandleGymFightAsync(int gymId, int trainerId, ulong ownerId)
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

        if (userId != ownerId)
        {
            var notYour = _localizationService.GetString("Gym.NotYourInteraction", language);
            await RespondAsync(notYour, ephemeral: true);
            return;
        }

        var player = await _dbContext.Players
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var trainer = await _dbContext.GymTrainers
            .Include(t => t.Gym)
            .FirstOrDefaultAsync(t => t.Id == trainerId && t.GymId == gymId);

        if (trainer is null)
        {
            await RespondAsync("This gym trainer could not be found.", ephemeral: true);
            return;
        }

        // Ensure the player is still in the same gym location
        var gym = trainer.Gym;
        if (gym.LocationId is not null && player.CurrentLocationId != gym.LocationId.Value)
        {
            var notHere = _localizationService.GetString("Gym.NotInThisLocation", language);
            await RespondAsync(notHere, ephemeral: true);
            return;
        }

        // Check if this trainer was already defeated by this player
        var existingProgress = await _dbContext.PlayerGymTrainerProgresses
            .FirstOrDefaultAsync(p => p.PlayerId == player.Id && p.GymTrainerId == trainer.Id);

        if (existingProgress is not null && existingProgress.Defeated)
        {
            var already = _localizationService.GetString("Gym.AlreadyDefeated", language);
            var alreadyMsg = string.Format(already, trainer.Name);
            await RespondAsync(alreadyMsg, ephemeral: true);
            return;
        }

        // Minimal battle flow: automatic victory with rewards
        if (existingProgress is null)
        {
            existingProgress = new PlayerGymTrainerProgress
            {
                PlayerId = player.Id,
                GymTrainerId = trainer.Id,
                Defeated = true,
                FirstDefeatedAtUtc = DateTime.UtcNow
            };
            _dbContext.PlayerGymTrainerProgresses.Add(existingProgress);
        }
        else
        {
            existingProgress.Defeated = true;
            if (existingProgress.FirstDefeatedAtUtc is null)
            {
                existingProgress.FirstDefeatedAtUtc = DateTime.UtcNow;
            }
        }

        // Apply rewards (money + optional item)
        var rewardMoney = trainer.RewardMoney;
        if (rewardMoney > 0)
        {
            player.Money += rewardMoney;
        }

        string responseText;

        if (trainer.RewardItemTypeId is not null && trainer.RewardItemQuantity > 0)
        {
            var itemType = await _dbContext.ItemTypes
                .FirstOrDefaultAsync(it => it.Id == trainer.RewardItemTypeId.Value);

            if (itemType is not null)
            {
                // Upsert inventory item
                var existingItem = player.Inventory
                    .FirstOrDefault(ii => ii.ItemTypeId == itemType.Id);

                if (existingItem is null)
                {
                    existingItem = new InventoryItem
                    {
                        PlayerId = player.Id,
                        ItemTypeId = itemType.Id,
                        Quantity = trainer.RewardItemQuantity
                    };
                    _dbContext.Add(existingItem);
                }
                else
                {
                    existingItem.Quantity += trainer.RewardItemQuantity;
                }

                var itemCode = itemType.Code;
                var itemIcon = itemType.IconCode;
                var nameKey = $"Item.{itemCode}.Name";
                var localizedName = _localizationService.GetString(nameKey, language);
                var itemName = string.IsNullOrEmpty(localizedName) || localizedName == nameKey
                    ? itemCode
                    : localizedName;

                if (rewardMoney > 0)
                {
                    var template = _localizationService.GetString("Gym.BattleWin.MoneyAndItem", language);
                    responseText = string.Format(template, trainer.Name, rewardMoney, trainer.RewardItemQuantity, $"{itemIcon} {itemName}");
                }
                else
                {
                    var template = _localizationService.GetString("Gym.BattleWin.ItemOnly", language);
                    responseText = string.Format(template, trainer.Name, trainer.RewardItemQuantity, $"{itemIcon} {itemName}");
                }
            }
            else
            {
                // Item type not found, fall back to money-only message
                var template = _localizationService.GetString("Gym.BattleWin.MoneyOnly", language);
                responseText = string.Format(template, trainer.Name, rewardMoney);
            }
        }
        else
        {
            var template = _localizationService.GetString("Gym.BattleWin.MoneyOnly", language);
            responseText = string.Format(template, trainer.Name, rewardMoney);
        }
        // Decide if we should offer a badge claim button (only for the gym leader, if badge not yet owned)
        ComponentBuilder? components = null;

        // Determine if this trainer is the leader (last in ordered list)
        var orderedTrainerIds = await _dbContext.GymTrainers
            .Where(t => t.GymId == gym.Id)
            .OrderBy(t => t.Order)
            .ThenBy(t => t.Name)
            .Select(t => t.Id)
            .ToListAsync();

        if (orderedTrainerIds.Count > 0 && orderedTrainerIds[^1] == trainer.Id)
        {
            var alreadyHasBadge = await _dbContext.PlayerGymBadges
                .AnyAsync(b => b.PlayerId == player.Id && b.GymId == gym.Id);

            if (!alreadyHasBadge)
            {
                components = new ComponentBuilder();
                var label = _localizationService.GetString("Gym.BadgeClaimButton", language);
                components.WithButton(label, $"gym_badge:{gym.Id}:{userId}", ButtonStyle.Success);
            }
        }

        await _dbContext.SaveChangesAsync();

        // Update the original /gym message so the trainer list and button reflect the new progress
        if (Context.Interaction is SocketMessageComponent componentInteraction)
        {
            // Reload gym with trainers to rebuild the list
            var gymWithTrainers = await _dbContext.Gyms
                .Include(g => g.Trainers)
                .Include(g => g.Location)
                .FirstOrDefaultAsync(g => g.Id == gym.Id);

            if (gymWithTrainers is not null)
            {
                var trainers = gymWithTrainers.Trainers
                    .OrderBy(t => t.Order)
                    .ThenBy(t => t.Name)
                    .ToList();

                // Load defeated trainers for this player
                var trainerIds = trainers.Select(t => t.Id).ToList();
                var progressList = await _dbContext.PlayerGymTrainerProgresses
                    .Where(p => p.PlayerId == player.Id && trainerIds.Contains(p.GymTrainerId) && p.Defeated)
                    .ToListAsync();
                var defeatedIds = new HashSet<int>(progressList.Select(p => p.GymTrainerId));

                var header = _localizationService.GetString("Gym.TrainerListHeader", language);
                var lineTemplate = _localizationService.GetString("Gym.TrainerLine", language);
                var leaderTemplate = _localizationService.GetString("Gym.LeaderLine", language);
                var defeatedPrefix = _localizationService.GetString("Gym.TrainerDefeatedPrefix", language);

                var lines = new List<string> { header };

                if (trainers.Count == 0)
                {
                    lines.Add("-");
                }
                else
                {
                    var index = 1;
                    foreach (var t in trainers)
                    {
                        var isLeader = index == trainers.Count;
                        string line;

                        if (isLeader)
                        {
                            var badge = gymWithTrainers.BadgeCode ?? string.Empty;
                            line = string.Format(leaderTemplate, index, t.Name, badge);
                        }
                        else
                        {
                            line = string.Format(lineTemplate, index, t.Name);
                        }

                        if (defeatedIds.Contains(t.Id))
                        {
                            line = defeatedPrefix + line;
                        }

                        lines.Add(line);
                        index++;
                    }
                }

                // Decide buttons based on remaining trainers and badge ownership
                var allDefeated = trainers.Count > 0 && trainers.All(t => defeatedIds.Contains(t.Id));
                var alreadyHasBadgeForGym = await _dbContext.PlayerGymBadges
                    .AnyAsync(b => b.PlayerId == player.Id && b.GymId == gymWithTrainers.Id);

                // Localized location name for title
                string locationName;
                if (gymWithTrainers.Location is not null && !string.IsNullOrWhiteSpace(gymWithTrainers.Location.Code))
                {
                    var locKey = $"Locations.{gymWithTrainers.Location.Code}.Name";
                    var localizedLoc = _localizationService.GetString(locKey, language);
                    locationName = localizedLoc == locKey ? gymWithTrainers.Location.Code : localizedLoc;
                }
                else
                {
                    locationName = gymWithTrainers.LocationId?.ToString() ?? gymWithTrainers.Id.ToString();
                }

                var titleTemplate = _localizationService.GetString("Gym.Title", language);
                var title = string.Format(titleTemplate, locationName);

                var embed = new EmbedBuilder()
                    .WithTitle(title)
                    .WithDescription(string.Join("\n", lines))
                    .WithColor(Color.DarkGreen)
                    .Build();

                var updatedComponents = new ComponentBuilder();

                if (allDefeated && !alreadyHasBadgeForGym)
                {
                    var label = _localizationService.GetString("Gym.BadgeClaimButton", language);
                    updatedComponents.WithButton(label, $"gym_badge:{gymWithTrainers.Id}:{userId}", ButtonStyle.Success);
                }
                else
                {
                    // Recalculate next trainer (first non-defeated) and show the fight button if any remain
                    var nextTrainer = trainers.FirstOrDefault(t => !defeatedIds.Contains(t.Id));
                    if (nextTrainer is not null)
                    {
                        var buttonLabel = _localizationService.GetString("Gym.NextFightButton", language);
                        updatedComponents.WithButton(buttonLabel, $"gym_fight:{gymWithTrainers.Id}:{nextTrainer.Id}:{userId}", ButtonStyle.Danger);
                    }
                }

                await componentInteraction.UpdateAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = updatedComponents.Build();
                });
            }

            // Also send a small ephemeral message with the battle result text
            await FollowupAsync(responseText, ephemeral: true);
        }
        else
        {
            // Fallback: just send the battle result
            await RespondAsync(responseText, ephemeral: true);
        }
    }

    [ComponentInteraction("gym_badge:*:*")]
    public async Task HandleGymBadgeClaimAsync(int gymId, ulong ownerId)
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

        if (userId != ownerId)
        {
            var notYour = _localizationService.GetString("Gym.NotYourInteraction", language);
            await RespondAsync(notYour, ephemeral: true);
            return;
        }

        var player = await _dbContext.Players
            .Include(p => p.GymBadges)
            .Include(p => p.CurrentLocation)
            .FirstOrDefaultAsync(p => p.GuildId == guildId && p.DiscordUserId == userId);

        if (player is null)
        {
            var mustStart = _localizationService.GetString("Adventure.MustStartFirst", language);
            await RespondAsync(mustStart, ephemeral: true);
            return;
        }

        var gym = await _dbContext.Gyms
            .Include(g => g.Location)
            .FirstOrDefaultAsync(g => g.Id == gymId);

        if (gym is null)
        {
            await RespondAsync("This gym could not be found.", ephemeral: true);
            return;
        }

        if (gym.LocationId is not null && player.CurrentLocationId != gym.LocationId.Value)
        {
            var notHere = _localizationService.GetString("Gym.NotInThisLocation", language);
            await RespondAsync(notHere, ephemeral: true);
            return;
        }

        var alreadyHasBadge = player.GymBadges.Any(b => b.GymId == gym.Id);
        if (alreadyHasBadge)
        {
            var already = _localizationService.GetString("Gym.BadgeAlreadyOwned", language);
            await RespondAsync(already, ephemeral: true);
            return;
        }

        var badge = new PlayerGymBadge
        {
            PlayerId = player.Id,
            GymId = gym.Id,
            ObtainedAtUtc = DateTime.UtcNow
        };

        _dbContext.PlayerGymBadges.Add(badge);
        await _dbContext.SaveChangesAsync();

        var badgeCode = gym.BadgeCode ?? gym.Code;
        var obtainedTemplate = _localizationService.GetString("Gym.BadgeObtained", language);
        var obtainedText = string.Format(obtainedTemplate, badgeCode);

        // Ephemeral confirmation for the player
        await RespondAsync(obtainedText, ephemeral: true);

        // Public announcement in the channel
        var orderedTrainerNames = await _dbContext.GymTrainers
            .Where(t => t.GymId == gym.Id)
            .OrderBy(t => t.Order)
            .ThenBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync();

        var leaderName = orderedTrainerNames.Count > 0
            ? orderedTrainerNames[^1]
            : gym.Code;

        string locationName;
        if (gym.Location is not null && !string.IsNullOrWhiteSpace(gym.Location.Code))
        {
            var locKey = $"Locations.{gym.Location.Code}.Name";
            var localizedLoc = _localizationService.GetString(locKey, language);
            locationName = localizedLoc == locKey ? gym.Location.Code : localizedLoc;
        }
        else
        {
            locationName = gym.LocationId?.ToString() ?? gym.Code;
        }

        var announcementTemplate = _localizationService.GetString("Gym.BadgeObtained.Announcement", language);
        var announcementText = string.Format(announcementTemplate,
            Context.User.Mention,
            leaderName,
            locationName,
            badgeCode);

        await Context.Channel.SendMessageAsync(announcementText);

        // Remove the claim button from the original /gym message
        if (Context.Interaction is SocketMessageComponent badgeComponent)
        {
            await badgeComponent.UpdateAsync(msg =>
            {
                msg.Components = new ComponentBuilder().Build();
            });
        }
    }
}
