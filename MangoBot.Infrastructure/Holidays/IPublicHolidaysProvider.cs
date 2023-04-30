namespace MangoBot.Infrastructure.Holidays;

public interface IPublicHolidaysProvider
{
    bool IsPublicHoliday(DateTime occurrence, string country);
}