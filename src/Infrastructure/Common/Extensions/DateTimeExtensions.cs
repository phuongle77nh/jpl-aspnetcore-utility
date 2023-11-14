namespace JPL.NetCoreUtility.Infrastructure.Common.Extensions;

public static class DateTimeExtensions
{
    public static List<DateTime> GetDaysBetween2Days(DateTime startDate, DateTime endDate)
    {
        return Enumerable.Range(0, 1 + endDate.Subtract(startDate).Days)
          .Select(offset => startDate.AddDays(offset))
          .ToList();
    }
}