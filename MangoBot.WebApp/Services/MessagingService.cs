using Discord.WebSocket;
using MangoBot.Infrastructure.DiscordMessaging;
using MangoBot.Infrastructure.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace MangoBot.WebApp.Services;

public class MessagingService : IHostedService
{
    private readonly IDiscordClientProvider discordClientProvider;
    private readonly ILogger<MessagingService> logger;
    private readonly IOptions<DiscordSettings> settings;
    private readonly IMediator mediator;
    private DiscordSocketClient? client;

    public MessagingService(
        IDiscordClientProvider discordClientProvider,
        ILogger<MessagingService> logger,
        IOptions<DiscordSettings> settings,
        IMediator mediator
        )
    {
        this.discordClientProvider = discordClientProvider;
        this.logger = logger;
        this.settings = settings;
        this.mediator = mediator;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Discord messaging service starting");
        this.client = await this.discordClientProvider.GetClient();
        this.client.MessageReceived += OnMessageReceived;
        this.logger.LogInformation("Discord messaging service started");
    }

    private async Task OnMessageReceived(SocketMessage messageEvent)
    {
        // Ignore messages from the bot itself.
        if (messageEvent.Author.Id == this.client?.CurrentUser.Id)
        {
            return;
        }
        
        this.logger.LogInformation("Message received: [{channel}][{user}]: {message}", messageEvent.Channel.Name, messageEvent.Author.Username, messageEvent.CleanContent);
        await mediator.Publish(new MessageNotification(messageEvent));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.client is not null)
        {
            this.client.MessageReceived -= OnMessageReceived;
        }
    }
}