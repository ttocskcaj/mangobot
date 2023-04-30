using System.Text.RegularExpressions;
using Discord;
using MangoBot.Infrastructure.DiscordMessaging;
using MangoBot.Infrastructure.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangoBot.Messaging.MessageHandlers;

public class BgMessageHandler : INotificationHandler<MessageNotification>
{
    private readonly IDiscordClientProvider clientProvider;
    private readonly ILogger<BgMessageHandler> logger;

    public BgMessageHandler(IDiscordClientProvider clientProvider, ILogger<BgMessageHandler> logger)
    {
        this.clientProvider = clientProvider;
        this.logger = logger;
    }
    
    public async Task Handle(MessageNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            if (!Regex.IsMatch(notification.Message.CleanContent, "[\\w\\s']+\\s(bg|BG)"))
            {
                this.logger.LogDebug("BgMessageHandler ignoring: {content}", notification.Message.CleanContent);
                return;
            }

            this.logger.LogDebug("BgMessageHandler handling: {content}", notification.Message.CleanContent);

            var client = await clientProvider.GetClient();
            var channel = await client.Rest.GetChannelAsync(notification.Message.Channel.Id) as ITextChannel;

            var sendMessageTask = channel?.SendMessageAsync($"No, {notification.Message.Author.Mention} BG",
                messageReference: new MessageReference(notification.Message.Id, notification.Message.Channel.Id));

            if (sendMessageTask != null)
            {
                await Task.WhenAll(sendMessageTask, notification.Message.AddReactionAsync(new Emoji("🥭")));
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected exception handling BG message");
        }
    }
}