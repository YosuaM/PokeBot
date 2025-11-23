using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PokeBotDiscord;

public class DiscordBotService : IHostedService
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;

    public DiscordBotService(
        ILogger<DiscordBotService> logger,
        IConfiguration configuration,
        DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider services)
    {
        _logger = logger;
        _configuration = configuration;
        _client = client;
        _interactionService = interactionService;
        _services = services;

        _client.Log += OnLogAsync;
        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Discord bot service");

        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            token = _configuration["Discord:Token"];
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogError("Discord token is not configured. Set DISCORD_TOKEN env var or Discord:Token in appsettings.json.");
            throw new InvalidOperationException("Discord token is not configured.");
        }

        await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Discord bot service");
        await _client.LogoutAsync();
        await _client.StopAsync();
    }

    private Task OnLogAsync(LogMessage message)
    {
        var logLevel = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, message.Exception, "{Source}: {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        _logger.LogInformation("Discord client is ready. Logged in as {User}", _client.CurrentUser);

        try
        {
            await _interactionService.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Slash commands registered globally.");

            // In development, you can register to a specific guild for faster updates:
            // await _interactionService.RegisterCommandsToGuildAsync(guildId: YOUR_GUILD_ID_HERE);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while registering slash commands.");
        }
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(context, _services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while executing interaction.");

            try
            {
                await interaction.GetOriginalResponseAsync();
            }
            catch
            {
                // ignored - no original response
            }
        }
    }
}
