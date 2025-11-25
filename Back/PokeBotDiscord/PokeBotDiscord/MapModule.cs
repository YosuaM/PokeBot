using System.IO;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;

namespace PokeBotDiscord;

public class MapModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IConfiguration _configuration;

    public MapModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [SlashCommand("map", "Show the Kanto region map")] 
    public async Task MapAsync()
    {
        if (Context.Guild is null)
        {
            // Mantener el mismo mensaje de solo-servidor que otros comandos
            await RespondAsync("This command can only be used inside a server.", ephemeral: true);
            return;
        }

        var imagePath = _configuration["Map:KantoImagePath"];

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            await RespondAsync("Map image path is not configured. Please set Map:KantoImagePath in appsettings.json.", ephemeral: true);
            return;
        }

        if (!File.Exists(imagePath))
        {
            await RespondAsync("The configured Kanto map image could not be found on the server.", ephemeral: true);
            return;
        }

        var fileName = Path.GetFileName(imagePath);

        // Crear embed con título y la imagen del mapa de Kanto
        var embed = new EmbedBuilder()
            .WithTitle("Kanto Map")
            .WithImageUrl($"attachment://{fileName}")
            .WithColor(Color.DarkBlue)
            .Build();

        await using var stream = File.OpenRead(imagePath);

        await RespondWithFileAsync(stream, fileName, embed: embed);
    }
}
