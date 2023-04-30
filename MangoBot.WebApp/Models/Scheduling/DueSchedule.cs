using MangoBot.Infrastructure.Models;

namespace MangoBot.WebApp.Models.Scheduling;

public class DueSchedule
{
    public Schedule Schedule { get; set; }

    public DateTime WhenDue { get; set; }
}