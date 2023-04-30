using System.Text.Json.Serialization;

namespace MangoBot.Infrastructure.Models;

public class Schedule
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string ScheduleExpression { get; set; } = "* * * * *";

    public string HolidayCalendar { get; set; }

    public string TimeZone { get; set; } = "Pacific/Auckland";

    public DateTime? LastRunUtc { get; set; }

    public Message Message { get; set; }

    public override string ToString() => Name;
}
