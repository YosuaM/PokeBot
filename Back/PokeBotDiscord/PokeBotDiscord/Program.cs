using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokeBotDiscord;
using PokeBotDiscord.Data;
using PokeBotDiscord.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDirectory);

var dbPath = builder.Configuration["Database:Path"];
if (string.IsNullOrWhiteSpace(dbPath))
{
	throw new InvalidOperationException("Database:Path must be configured in appsettings.json.");
}

// If Database:Path is relative, combine with data directory
if (!Path.IsPathRooted(dbPath))
{
	dbPath = Path.Combine(dataDirectory, dbPath);
}

Console.WriteLine($"[PokeBot] Using database path: {dbPath}");
var connectionString = $"Data Source={dbPath}";

builder.Services.AddDbContext<PokeBotDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<ILocalizationService, LocalizationService>();

builder.Services.AddSingleton(sp =>
{
    var config = new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildPresences,
        LogGatewayIntentWarnings = true,
    };

    return new DiscordSocketClient(config);
});

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<DiscordSocketClient>();
    return new InteractionService(client.Rest);
});

builder.Services.AddHostedService<DiscordBotService>();

var host = builder.Build();

await host.RunAsync();