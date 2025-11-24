using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;
using PokeBotDiscord.Services;

namespace PokeBotDiscord;

[Group("settings", "Server configuration commands")]
public class SettingsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILocalizationService _localizationService;
    private readonly PokeBotDbContext _dbContext;

    public SettingsModule(ILocalizationService localizationService, PokeBotDbContext dbContext)
    {
        _localizationService = localizationService;
        _dbContext = dbContext;
    }

    [SlashCommand("language", "Set the bot language for this server")] 
    public async Task SetLanguageAsync(string lang)
    {
        if (Context.Guild is null)
        {
            var message = _localizationService.GetString("Language.Set.ForGuildOnly", "en");
            await RespondAsync(message, ephemeral: true);
            return;
        }

        lang = lang.ToLowerInvariant();
        if (lang != "en" && lang != "es")
        {
            var invalid = _localizationService.GetString("Language.Set.Invalid", GetCurrentGuildLanguage());
            await RespondAsync(invalid, ephemeral: true);
            return;
        }

        await _localizationService.SetGuildLanguageAsync(Context.Guild.Id, lang);

        var key = lang switch
        {
            "es" => "Language.Set.Success.es",
            _ => "Language.Set.Success.en"
        };

        var confirm = _localizationService.GetString(key, lang);
        await RespondAsync(confirm, ephemeral: true);
    }

    [SlashCommand("stamina", "Set how many stamina points are recovered per hour for this server (1-5)")]
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

    private string GetCurrentGuildLanguage()
    {
        return Context.Guild is null
            ? "en"
            : _localizationService.GetGuildLanguage(Context.Guild.Id);
    }
}
