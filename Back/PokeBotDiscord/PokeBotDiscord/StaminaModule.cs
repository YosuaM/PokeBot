using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

public class StaminaModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILocalizationService _localizationService;

    public StaminaModule(PokeBotDbContext dbContext, ILocalizationService localizationService)
    {
        _dbContext = dbContext;
        _localizationService = localizationService;
    }

    [SlashCommand("set-stamina-recovery", "Set how many stamina points are recovered per hour for this server (1-5)")]
    public async Task SetStaminaPerHourAsync(int staminaPerHour)
    {
        if (Context.Guild is null)
        {
            var msg = _localizationService.GetString("Adventure.ForGuildOnly", "en");
            await RespondAsync(msg, ephemeral: true);
            return;
        }

        var guildId = Context.Guild.Id;
        var language = _localizationService.GetGuildLanguage(guildId);

        if (staminaPerHour < 1 || staminaPerHour > 5)
        {
            var invalid = _localizationService.GetString("Stamina.Set.Invalid", language);
            await RespondAsync(invalid, ephemeral: true);
            return;
        }

        var settings = await _dbContext.GuildSettings
            .FirstOrDefaultAsync(gs => gs.GuildId == guildId);

        if (settings is null)
        {
            settings = new GuildSettings
            {
                GuildId = guildId,
                Language = language,
                StaminaPerHour = staminaPerHour
            };

            _dbContext.GuildSettings.Add(settings);
        }
        else
        {
            settings.StaminaPerHour = staminaPerHour;
        }

        await _dbContext.SaveChangesAsync();

        var successTemplate = _localizationService.GetString("Stamina.Set.Success", language);
        var success = string.Format(successTemplate, staminaPerHour);

        await RespondAsync(success, ephemeral: true);
    }
}
