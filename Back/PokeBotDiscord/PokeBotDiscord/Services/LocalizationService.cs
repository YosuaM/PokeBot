using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PokeBotDiscord.Data;
using PokeBotDiscord.Data.Entities;

namespace PokeBotDiscord.Services;

public class LocalizationService : ILocalizationService
{
    private readonly PokeBotDbContext _dbContext;
    private readonly ILogger<LocalizationService> _logger;

    private readonly Dictionary<string, string> _en = new();
    private readonly Dictionary<string, string> _es = new();
    private bool _loaded;
    private readonly object _lock = new();

    private const string DefaultLanguage = "en";

    public LocalizationService(PokeBotDbContext dbContext, ILogger<LocalizationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public string GetString(string key, string languageCode)
    {
        EnsureLoaded();

        var lang = string.IsNullOrWhiteSpace(languageCode) ? DefaultLanguage : languageCode.ToLowerInvariant();

        var dict = lang switch
        {
            "es" => _es,
            _ => _en
        };

        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }

        // Fallback to English if key missing in selected language
        if (lang != DefaultLanguage && _en.TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        _logger.LogWarning("Missing localization key '{Key}' for language '{Language}'", key, lang);
        return key;
    }

    public string GetGuildLanguage(ulong guildId)
    {
        if (guildId == 0)
        {
            return DefaultLanguage;
        }

        var settings = _dbContext.Set<GuildSettings>().AsNoTracking().FirstOrDefault(x => x.GuildId == guildId);
        return string.IsNullOrWhiteSpace(settings?.Language) ? DefaultLanguage : settings!.Language.ToLowerInvariant();
    }

    public async Task SetGuildLanguageAsync(ulong guildId, string languageCode)
    {
        if (guildId == 0)
        {
            return;
        }

        var lang = languageCode.ToLowerInvariant();
        if (lang != "en" && lang != "es")
        {
            throw new ArgumentException("Unsupported language", nameof(languageCode));
        }

        var settings = await _dbContext.Set<GuildSettings>().FirstOrDefaultAsync(x => x.GuildId == guildId);

        if (settings is null)
        {
            settings = new GuildSettings
            {
                GuildId = guildId,
                Language = lang
            };
            await _dbContext.AddAsync(settings);
        }
        else
        {
            settings.Language = lang;
        }

        await _dbContext.SaveChangesAsync();
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (_lock)
        {
            if (_loaded)
            {
                return;
            }

            try
            {
                LoadLanguage("en", _en);
                LoadLanguage("es", _es);
                _loaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load localization files.");
            }
        }
    }

    private void LoadLanguage(string languageCode, Dictionary<string, string> target)
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "Data", "Localization", $"{languageCode}.json");

        if (!File.Exists(path))
        {
            _logger.LogWarning("Localization file not found: {Path}", path);
            return;
        }

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (data is null)
        {
            _logger.LogWarning("Localization file {Path} could not be deserialized.", path);
            return;
        }

        target.Clear();
        foreach (var kvp in data)
        {
            target[kvp.Key] = kvp.Value;
        }
    }
}
