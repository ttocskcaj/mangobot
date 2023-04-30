using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MangoBot.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MangoBot.Infrastructure.DiscordMessaging;

public class DiscordClientProvider : IDiscordClientProvider
{
    private readonly ILogger<DiscordClientProvider> logger;
    private readonly DiscordSettings settings;
    private readonly DiscordSocketClient client;

    public DiscordClientProvider(ILogger<DiscordClientProvider> logger, IOptions<DiscordSettings> settings)
    {
        this.logger = logger;
        this.settings = settings.Value;
        this.client = new DiscordSocketClient(new DiscordSocketConfig
        {
            FormatUsersInBidirectionalUnicode = false,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
        });
        this.client.Log += HandleDiscordLogEvent;
    }
    
    public async Task<DiscordSocketClient> GetClient()
    {
        await this.EnsureConnected();
        
        return client;
    }

    private async Task EnsureConnected()
    {
        if (this.client.ConnectionState != ConnectionState.Connected)
        {
            await this.client.LoginAsync(TokenType.Bot, settings.Token);
            await this.client.StartAsync(); 
        }
    }

    private async Task HandleDiscordLogEvent(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                this.logger.LogCritical(message.Message, message.Exception);
                break;
            case LogSeverity.Error:
                this.logger.LogError(message.Message, message.Exception);
                break;
            case LogSeverity.Warning:
                this.logger.LogWarning(message.Message, message.Exception);
                break;
            case LogSeverity.Info:
                this.logger.LogInformation(message.Message, message.Exception);
                break;
            case LogSeverity.Verbose:
                this.logger.LogTrace(message.Message, message.Exception);
                break;
            case LogSeverity.Debug:
                this.logger.LogDebug(message.Message, message.Exception);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}