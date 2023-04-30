using MangoBot.Infrastructure.Contexts;
using MangoBot.Infrastructure.DiscordMessaging;
using MangoBot.Infrastructure.Holidays;
using MangoBot.Infrastructure.Models;
using MangoBot.WebApp.Models.Scheduling;
using Microsoft.EntityFrameworkCore;

namespace MangoBot.WebApp.Services;

public class SchedulingService : IHostedService
{
    private readonly ILogger<SchedulingService> logger;
    private readonly IMessageSender messageSender;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IPublicHolidaysProvider publicHolidays;
    private const int SecondsBetweenChecks = 600;

    public SchedulingService(
        ILogger<SchedulingService> logger,
        IMessageSender messageSender,
        IServiceScopeFactory serviceScopeFactory,
        IPublicHolidaysProvider publicHolidays)
    {
        this.logger = logger;
        this.messageSender = messageSender;
        this.serviceScopeFactory = serviceScopeFactory;
        this.publicHolidays = publicHolidays;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Starting scheduling service");

        while (!cancellationToken.IsCancellationRequested)
        {
            this.logger.LogInformation("Running schedule check");

            var nextCheckAt = DateTime.UtcNow.AddSeconds(SecondsBetweenChecks);
            
            using (var scope = this.serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SchedulesContext>();
                var schedules = await dbContext.Schedules.ToListAsync(cancellationToken: cancellationToken);
                
                var scheduleTasks = new List<Task>();

                foreach (var schedule in schedules)
                {
                    var cronExpression = Cronos.CronExpression.Parse(schedule.ScheduleExpression);
                    var lastRun = schedule.LastRunUtc ?? DateTime.UtcNow;
                    var occurrences = 
                        cronExpression.GetOccurrences(lastRun, nextCheckAt, TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZone))
                            .Where(_ => !publicHolidays.IsPublicHoliday(_, schedule.HolidayCalendar))
                            .ToList();

                    this.logger.LogInformation($"{occurrences.Count} occurrences due in the next {SecondsBetweenChecks} seconds for schedule '{schedule.Name}'");

                    var historicRunCompleted = false;
                    
                    foreach (var occurrence in occurrences)
                    {
                        var isHistoric = occurrence < DateTime.UtcNow;

                        // Only trigger one historic occurence
                        if (isHistoric && historicRunCompleted)
                        {
                            continue;
                        }
                        
                        var dueSchedule = new DueSchedule
                        {
                            Schedule = schedule,
                            WhenDue = occurrence,
                        };
                        
                        scheduleTasks.Add(Task.Run(async () =>
                        {
                            var delay = TimeSpan.Zero;
                            if (!isHistoric)
                            {
                                delay = occurrence - DateTime.UtcNow;
                            }
                            this.logger.LogInformation($"Waiting {delay.TotalSeconds} seconds before triggering schedule '{schedule.Name}' @ {occurrence:O}");
                            await Task.Delay(delay, cancellationToken);
                            await OnScheduleDue(dueSchedule);
                            schedule.LastRunUtc = DateTime.UtcNow;
                            dbContext.Update(schedule);
                            await dbContext.SaveChangesAsync(cancellationToken);
                        }, cancellationToken));
                        
                        if (isHistoric)
                        {
                            historicRunCompleted = true;
                        }
                    }
                }

                await Task.WhenAll(scheduleTasks);
            }

            // Wait until the next check is due
            var timeUntilNextCheck = nextCheckAt - DateTime.UtcNow;
            if (timeUntilNextCheck > TimeSpan.Zero)
            {
                await Task.Delay(timeUntilNextCheck, cancellationToken);
            }
        }
    }

    private async Task OnScheduleDue(DueSchedule dueSchedule)
    {
        this.logger.LogInformation($"Schedule '{dueSchedule.Schedule.Name}' triggered");

        await this.messageSender.SendToChannel(dueSchedule.Schedule.Message);
    }

    private static DateTime? NextScheduleInRange(Schedule schedule, DateTime start, DateTime end)
    {
        return Cronos.CronExpression.Parse(schedule.ScheduleExpression)
            .GetOccurrences(start, end)
            .FirstOrDefault();
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Stopping scheduling service");
    }
}
