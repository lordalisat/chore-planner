namespace Chores.Domain;

/// <summary>
/// The day a Recurring Chore would ideally be done on: a weekday, or the first or last of the month for an Interval in
/// months or years. Weekdays share their values with <see cref="DayOfWeek"/>.
/// </summary>
public enum PreferredDay
{
    Sunday = DayOfWeek.Sunday,
    Monday = DayOfWeek.Monday,
    Tuesday = DayOfWeek.Tuesday,
    Wednesday = DayOfWeek.Wednesday,
    Thursday = DayOfWeek.Thursday,
    Friday = DayOfWeek.Friday,
    Saturday = DayOfWeek.Saturday,
    // Pinned, as they are stored
    FirstOfMonth = 7,
    LastOfMonth = 8,
}

internal static class PreferredDays
{
    /// <summary>First or last of month, which only an Interval in months or years allows.</summary>
    public static bool IsInMonth(this PreferredDay day) => day is PreferredDay.FirstOfMonth or PreferredDay.LastOfMonth;

    /// <summary>The first such day on or after a date.</summary>
    public static DateOnly OnOrAfter(this PreferredDay day, DateOnly date) => day switch
    {
        PreferredDay.FirstOfMonth => date.Day == 1 ? date : FirstOfMonth(date).AddMonths(1),
        PreferredDay.LastOfMonth => FirstOfMonth(date).AddMonths(1).AddDays(-1),
        _ => date.AddDays(((int)day - (int)date.DayOfWeek + 7) % 7),
    };

    /// <summary>The last such day on or before a date.</summary>
    public static DateOnly OnOrBefore(this PreferredDay day, DateOnly date) => day switch
    {
        PreferredDay.FirstOfMonth => FirstOfMonth(date),
        PreferredDay.LastOfMonth => date.AddDays(1).Day == 1 ? date : FirstOfMonth(date).AddDays(-1),
        _ => date.AddDays(-(((int)date.DayOfWeek - (int)day + 7) % 7)),
    };

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
}
