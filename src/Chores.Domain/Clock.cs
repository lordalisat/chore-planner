namespace Chores.Domain;

public static class Clock
{
    private static readonly TimeZoneInfo Amsterdam = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    /// <summary>The current date in Europe/Amsterdam.</summary>
    public static DateOnly Today(this TimeProvider clock) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Amsterdam).DateTime);
}
