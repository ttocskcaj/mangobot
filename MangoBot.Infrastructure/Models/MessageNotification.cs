using Discord.WebSocket;
using MediatR;

namespace MangoBot.Infrastructure.Models;

public class MessageNotification : INotification
{
    public MessageNotification(SocketMessage messageEvent)
    {
        this.Message = messageEvent;
    }

    public SocketMessage Message { get; }
}