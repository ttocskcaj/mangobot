namespace MangoBot.WebApp.Services;

using System.Text;
using System.Timers;
using Discord;
using Discord.Rest;
using MangoBot.Infrastructure.DiscordMessaging;
using MangoBot.Infrastructure.Models;
using Microsoft.Extensions.Options;
using OpenAI_API.Completions;
using OpenAI_API.Models;

public class BoredService : IHostedService
{
    private readonly IDiscordClientProvider clientProvider;
    private readonly IMessageSender messageSender;
    private readonly ILogger<BoredService> logger;
    private readonly IOptions<DiscordSettings> settings;
    private static int minutesBetweenChecks = 10;
    private static int minutesToBoredom = 30;

    private static TimeOnly startTime = new(09,00);
    private static TimeOnly endTime = new(22,00);
    
    private readonly Timer timer;
    
    private static List<string> topics = new List<string>
            {
                "World Capitals",
                "Famous Paintings",
                "Classic Literature",
                "Movie Quotes",
                "Science Fiction",
                "Historical Events",
                "Musical Instruments",
                "Animal Kingdom",
                "Inventors and Inventions",
                "Space Exploration",
                "Mythical Creatures",
                "Famous Landmarks",
                "Culinary Arts",
                "Sports Trivia",
                "Art and Artists",
                "Geography",
                "Pop Culture",
                "Languages of the World",
                "Mathematics",
                "Technology",
                "Celebrities",
                "Literary Characters",
                "Music Genres",
                "Ancient Civilizations",
                "Weather Phenomena",
                "Gaming History",
                "Natural Wonders",
                "World Religions",
                "Human Anatomy",
                "Botany",
                "Fashion Through the Ages",
                "Famous Quotes",
                "Exploration and Discoveries",
                "Mythology",
                "Trivia",
                "Astronomy",
                "History of Science",
                "Architecture Styles",
                "Car Models and Brands",
                "Political Leaders",
                "Olympic Games",
                "Epic Fantasy Series",
                "Musical Composers",
                "Modern Art Movements",
                "Fashion Designers",
                "Chemical Elements",
                "Word Origins",
                "World Records",
                "Monuments of the World",
                "Endangered Species",
                "Medical Breakthroughs",
                "Astrology",
                "Board Games",
                "Superheroes",
                "Space Missions",
                "Greatest Novels",
                "Ancient Philosophers",
                "Ancient Mythologies",
                "TV Show Trivia",
                "Emperors and Kings",
                "Natural Disasters",
                "Historical Figures",
                "World Currencies",
                "National Holidays",
                "Famous Scientists",
                "Musical Bands",
                "Political Ideologies",
                "Famous Explorers",
                "Environmental Issues",
                "Hollywood Stars",
                "Human Psychology",
                "Famous Inventors",
                "Famous Battles",
                "Fashion Icons",
                "Mythical Places",
                "Literary Quotes",
                "Geological Formations",
                "Renaissance Art",
                "Computer Programming",
                "Great Philosophical Works",
                "Marvel vs. DC",
                "Human Rights Movements",
                "Wonders of the Ancient World",
            };

    public BoredService(IDiscordClientProvider clientProvider, IMessageSender messageSender, ILogger<BoredService> logger, IOptions<DiscordSettings> settings)
    {
        this.clientProvider = clientProvider;
        this.messageSender = messageSender;
        this.logger = logger;
        this.settings = settings;
        this.timer = new Timer(TimeSpan.FromMinutes(minutesBetweenChecks));
        this.timer.Elapsed += (sender, args) => this.OnTimer().GetAwaiter().GetResult();
        this.timer.AutoReset = true;
        this.timer.Enabled = true;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await this.OnTimer();
        this.timer.Start();
        this.logger.LogInformation("Bored Service started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this.timer.Stop();
        this.logger.LogInformation("Bored Service stopped");

        return Task.CompletedTask;
    }
    
    private async Task OnTimer()
    {
        this.logger.LogDebug("Bored Service timer triggered");
        
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
        var now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);
        if (!TimeOnly.FromDateTime(now).IsBetween(startTime, endTime))
        {
            this.logger.LogDebug("Skipping bored message - current time '{Now}' is outside allowed time", now);

            return;
        }

        
        try
        {
            var channel = await this.messageSender.GetGuildChannelByName(this.settings.Value.GuildName, "og-only");

            if (channel is null)
            {
                this.logger.LogError("Channel 'og-only' not found");
                return;
            }

            var latestMessage = await this.GetLatestMessage(channel);
            if (this.IsMessageWithin(latestMessage, TimeSpan.FromMinutes(minutesToBoredom)))
            {
                this.logger.LogDebug("Skipping bored message - Latest message '{LatestMessageContent}' is within '{MinutesBetweenChecks}' minutes", latestMessage.Content, minutesBetweenChecks);
                return;
            }

            var messageAge = DateTimeOffset.UtcNow.Subtract(latestMessage.Timestamp);
            this.logger.LogDebug("Last message was {MessageAge} ago", messageAge);

            await this.GenerateAndSendMessage();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Exception executing timer: {ExMessage}", ex.Message);
        }
        
    }

    private async Task<RestMessage> GetLatestMessage(RestTextChannel restTextChannel)
    {
        var client = await this.clientProvider.GetClient();
        var messagePage = await restTextChannel.GetMessagesAsync(limit: 1).FirstOrDefaultAsync();
        if (messagePage is null)
        {
            throw new Exception("No messages");
        }
        
        var latestMessage = messagePage.FirstOrDefault();
        if (latestMessage is null)
        {
            throw new Exception("No messages");
        }

        return latestMessage;
    }

    private bool IsMessageWithin(IMessage message, TimeSpan timeSpan) =>
        DateTimeOffset.Compare(
            message.Timestamp, 
            DateTimeOffset.UtcNow - timeSpan)
        != -1;

    private async Task GenerateAndSendMessage()
    {
        var users = PickRandomItems(
            await this.messageSender.GetGuildChannelMembers(this.settings.Value.GuildName, "og-only"),
            2)
            .Select(_ => _.DisplayName)
            .ToList();

        var prompt = this.GetPrompt(users);
        
        var api = new OpenAI_API.OpenAIAPI(settings.Value.OpenAiKey);
        var message = new StringBuilder();
            
        await foreach (var token in api.Completions.StreamCompletionEnumerableAsync(new CompletionRequest(prompt, Model.DavinciText, 1000, 0.5, presencePenalty: 0.1, frequencyPenalty: 0.1)))
        {
            message.Append(token);
        }

        await this.messageSender.SendToChannel(new Message
        {
            Channel = "og-only",
            MessageBody = message.ToString(),
        });
    }
    
    private string GetPrompt(List<string> targets)
    {
        var list = new List<string>
        {
            $"A pickup line that a robot might use. Addressed to {targets.First()}:",
            $"An interesting fact about {GetRandomTopic()}:",
            $"A haiku about {GetRandomTopic()}:",
            $"A conversation starter about {GetRandomTopic()}. Don't put it in quotes:",
            $"A poem about {GetRandomTopic()} with {targets[0]} and {targets[1]}:",
            "A random salesforce code sample for Joe to review:",
            "A controversial anime take:",
            $"A joke about {GetRandomTopic()}:",
        };
        
        var random = new Random();
        var randomIndex = random.Next(list.Count);

        return list[randomIndex];
    }
    
    static IEnumerable<T> PickRandomItems<T>(IEnumerable<T> source, int count)
    {
        var random = new Random();
        var randomItems = source.OrderBy(item => random.Next()).Take(count).ToList();
        return randomItems;
    }

    private static string GetRandomTopic()
    {
        var random = new Random();
        var index = random.Next(topics.Count);
        return topics[index];
    }
}
