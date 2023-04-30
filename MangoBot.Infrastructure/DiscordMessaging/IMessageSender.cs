using MangoBot.Infrastructure.Models;

namespace MangoBot.Infrastructure.DiscordMessaging;

public interface IMessageSender
{
    Task SendToChannel(Message message);
}