using MangoBot.Infrastructure.Models;

namespace MangoBot.Infrastructure.DiscordMessaging;

using Discord.Rest;

public interface IMessageSender
{
    Task SendToChannel(Message message);
    Task<RestGuild?> GetGuildByName(string guildName);
    Task<RestTextChannel?> GetGuildChannelByName(RestGuild guild, string channelName);
    Task<RestTextChannel?> GetGuildChannelByName(string guildName, string channelName);
    Task<IEnumerable<RestGuildUser>> GetGuildChannelMembers(string valueGuildName, string botTesting);
}