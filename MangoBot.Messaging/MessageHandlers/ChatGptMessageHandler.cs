using System.Text;
using System.Text.RegularExpressions;
using Discord;
using MangoBot.Infrastructure.DiscordMessaging;
using MangoBot.Infrastructure.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI_API.Completions;
using OpenAI_API.Models;

namespace MangoBot.Messaging.MessageHandlers;

public class ChatGptMessageHandler : INotificationHandler<MessageNotification>
{
    private readonly IDiscordClientProvider clientProvider;
    private readonly ILogger<BgMessageHandler> logger;
    private readonly IOptions<DiscordSettings> settings;

    public ChatGptMessageHandler(IDiscordClientProvider clientProvider, ILogger<BgMessageHandler> logger, IOptions<DiscordSettings> settings)
    {
        this.clientProvider = clientProvider;
        this.logger = logger;
        this.settings = settings;
    }
    
    public async Task Handle(MessageNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var match = Regex.Match(notification.Message.CleanContent, "uwu", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                this.logger.LogDebug("ChatGptMessageHandler ignoring: {content}", notification.Message.CleanContent);
                return;
            }

            this.logger.LogDebug("ChatGptMessageHandler handling: {content}", notification.Message.CleanContent);

            var client = await clientProvider.GetClient();
            var channel = await client.Rest.GetChannelAsync(notification.Message.Channel.Id) as ITextChannel;

            
            var api = new OpenAI_API.OpenAIAPI(settings.Value.OpenAiKey);
            var message = new StringBuilder();
            
            await foreach (var token in api.Completions.StreamCompletionEnumerableAsync(new CompletionRequest(this.GetPrompt(notification.Message.Author.Username), Model.DavinciText, 1000, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1)).WithCancellation(cancellationToken))
            {
                message.Append(token);
            }

            await channel!.SendMessageAsync(message.ToString(), messageReference: new MessageReference(notification.Message.Id, notification.Message.Channel.Id));
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected exception handling Open AI message");
        }
    }

    private string GetPrompt(string author)
    {
        var list = new List<string>
        {
            $"The most cringy cutesy uwu filled compliment addressed to {author}. Don't put it in quotes:",
            $"An lame insult that a child would call someone. Addressed to {author}:",
        };
        
        var random = new Random();
        var randomIndex = random.Next(list.Count);

        return list[randomIndex];
    }
}
