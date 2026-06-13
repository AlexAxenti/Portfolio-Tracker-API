namespace PortfolioTracker.Api.Common;

public static class DateTimeHelpers
{
    public static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime? NormalizeUtcOrNull(DateTime? value)
    {
        return value is null ? null : NormalizeUtc(value.Value);
    }
}
