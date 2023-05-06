using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Discord;
using MangoBot.Infrastructure.DiscordMessaging;
using MangoBot.Infrastructure.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PexelsDotNetSDK.Api;

namespace MangoBot.Messaging.MessageHandlers;

public class CattoMessageHandler : INotificationHandler<MessageNotification>
{
    private readonly IDiscordClientProvider clientProvider;
    private readonly ILogger<BgMessageHandler> logger;
    private readonly IOptions<DiscordSettings> settings;
    private readonly HttpClient client;
    private readonly Random random;

    public CattoMessageHandler(IDiscordClientProvider clientProvider, ILogger<BgMessageHandler> logger, IOptions<DiscordSettings> settings)
    {
        this.clientProvider = clientProvider;
        this.logger = logger;
        this.settings = settings;
        this.random = new Random();
    }
    
    public async Task Handle(MessageNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var match = Regex.Match(notification.Message.CleanContent, "^/catto", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                this.logger.LogDebug("CattoMessageHandler ignoring: {content}", notification.Message.CleanContent);
                return;
            }

            this.logger.LogDebug("CattoMessageHandler handling: {content}", notification.Message.CleanContent);

            var client = await clientProvider.GetClient();
            var channel = await client.Rest.GetChannelAsync(notification.Message.Channel.Id) as ITextChannel;

            await channel!.SendMessageAsync(await this.GetRandomCatImageUrlAsync());
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected exception handling CattoMessageHandler");
        }
    }

    private async Task<string> GetRandomCatImageUrlAsync()
    {
        var pexelsClient = new PexelsClient(this.settings.Value.PexelApiKey);
        var page = await pexelsClient.SearchPhotosAsync("horror", page: random.Next(1, 100), pageSize: 1);

        var image = page.photos.FirstOrDefault();

        return image.source.original;
    }

}