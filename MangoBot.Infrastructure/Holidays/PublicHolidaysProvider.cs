using PublicHoliday;

namespace MangoBot.Infrastructure.Holidays;

public class PublicHolidaysProvider : IPublicHolidaysProvider
{
    public bool IsPublicHoliday(DateTime occurrence, string country)
    {
        return GetHolidayImplementation(country).IsPublicHoliday(occurrence);
    }

    private IPublicHolidays GetHolidayImplementation(string holidayCalendar)
    {
        return holidayCalendar switch
        {
            "NZ" => new NewZealandPublicHoliday(),
            _ => throw new ArgumentOutOfRangeException($"Calendar '{holidayCalendar}' not implemented")
        };
    }
}