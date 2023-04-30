using Discord.Rest;
using MangoBot.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MangoBot.Infrastructure.DiscordMessaging;

public class DiscordMessageSender : IMessageSender
{
    private readonly IDiscordClientProvider discordClientProvider;
    private readonly ILogger<DiscordMessageSender> logger;
    private readonly DiscordSettings settings;

    public DiscordMessageSender(
        IDiscordClientProvider discordClientProvider,
        ILogger<DiscordMessageSender> logger,
        IOptions<DiscordSettings> settings)
    {
        this.discordClientProvider = discordClientProvider;
        this.logger = logger;
        this.settings = settings.Value;
    }
    
    public async Task SendToChannel(Message message)
    {
        var client = await this.discordClientProvider.GetClient();
        var guilds = await client.Rest.GetGuildsAsync();
        var guild = guilds.FirstOrDefault(_ => _.Name == this.settings.GuildName);

        if (guild is null)
        {
            this.logger.LogError("Could not send message. Guild '{GuildName}' not found", this.settings.GuildName);
            return;
        }
        
        var channels = await guild.GetTextChannelsAsync();
        var channel = channels.FirstOrDefault(_ => _.Name == message.Channel);

        if (channel == null)
        {
            this.logger.LogError("Could not send message. Channel {Channel} not found", message.Channel);
            return;
        }

        var mentions = (await GetMentions(channel, message.Tags)).ToList();
        if (mentions.Any())
        {
            await channel.SendMessageAsync($"{string.Join(" ", mentions)}\n {message.MessageBody}");
        }
        else
        {
            await channel.SendMessageAsync(message.MessageBody);
        }
    }

    private static async Task<IEnumerable<string>> GetMentions(RestTextChannel channel, List<string>? tags)
    {
        if (tags is null)
        {
            return Enumerable.Empty<string>();
        }
        
        var allUsers = await channel.GetUsersAsync().FirstOrDefaultAsync();
        if (allUsers is null)
        {
            return Enumerable.Empty<string>();
        }

        return tags
            .Select(tag => allUsers.FirstOrDefault(_ => _.ToString() == tag))
            .Where(_ => _ != null)
            .Cast<RestGuildUser>()
            .Select(_ => _.Mention);
    }
}