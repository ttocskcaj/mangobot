using System.Text.Json;
using System.Text.RegularExpressions;
using Discord;
using MangoBot.Infrastructure.DiscordMessaging;
using MangoBot.Infrastructure.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MangoBot.Messaging.MessageHandlers;

public class DoggoMessageHandler : INotificationHandler<MessageNotification>
{
    private readonly IDiscordClientProvider clientProvider;
    private readonly ILogger<BgMessageHandler> logger;
    private readonly IOptions<DiscordSettings> settings;
    private readonly HttpClient client;

    public DoggoMessageHandler(IDiscordClientProvider clientProvider, ILogger<BgMessageHandler> logger, IOptions<DiscordSettings> settings)
    {
        this.clientProvider = clientProvider;
        this.logger = logger;
        this.settings = settings;
        this.client = new HttpClient();
    }
    
    public async Task Handle(MessageNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var match = Regex.Match(notification.Message.CleanContent, "^/doggo", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                this.logger.LogDebug("DoggoMessageHandler ignoring: {content}", notification.Message.CleanContent);
                return;
            }

            this.logger.LogDebug("DoggoMessageHandler handling: {content}", notification.Message.CleanContent);

            var client = await clientProvider.GetClient();
            var channel = await client.Rest.GetChannelAsync(notification.Message.Channel.Id) as ITextChannel;

            await channel!.SendMessageAsync(await this.GetRandomDogImageUrlAsync());
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected exception handling DoggoMessageHandler");
        }
    }

    private async Task<string> GetRandomDogImageUrlAsync()
    {
        const string url = "https://dog.ceo/api/breeds/image/random";
        
        var response = await this.client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        var imageUrl = root.GetProperty("message").GetString();
        
        return imageUrl;
    }

}