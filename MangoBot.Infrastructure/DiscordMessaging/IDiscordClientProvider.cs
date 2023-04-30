using Discord.WebSocket;

namespace MangoBot.Infrastructure.DiscordMessaging;

public interface IDiscordClientProvider
{
    Task<DiscordSocketClient> GetClient();
}