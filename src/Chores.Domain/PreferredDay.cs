namespace Chores.Domain;

/// <summary>
/// The day a Recurring Chore would ideally be done on: a weekday, or a day in the month for an Interval in months or
/// years.
/// </summary>
public abstract record PreferredDay
{
    // Closed to the kinds declared here
    private PreferredDay()
    {
    }

    public static implicit operator PreferredDay(DayOfWeek day) => new Weekday(day);

    /// <summary>The first such day on or after a date.</summary>
    public abstract DateOnly OnOrAfter(DateOnly date);

    /// <summary>The last such day on or before a date.</summary>
    public abstract DateOnly OnOrBefore(DateOnly date);

    public virtual bool Allows(Interval interval) => true;

    /// <summary>The nearest such day to a date; ties go to the later one.</summary>
    public DateOnly NearestTo(DateOnly date)
    {
        DateOnly before = OnOrBefore(date);
        DateOnly after = OnOrAfter(date);
        return date.DayNumber - before.DayNumber < after.DayNumber - date.DayNumber ? before : after;
    }

    /// <summary>A day of the week, which needs an Interval of at least a week.</summary>
    public sealed record Weekday(DayOfWeek Day) : PreferredDay
    {
        // Moving to the nearest one, up to three days either way, would swamp a shorter Interval
        public override bool Allows(Interval interval) => interval.Unit is not IntervalUnit.Day || interval.Count >= 7;

        public override DateOnly OnOrAfter(DateOnly date) => date.AddDays(((int)Day - (int)date.DayOfWeek + 7) % 7);

        public override DateOnly OnOrBefore(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek - (int)Day + 7) % 7));
    }

    /// <summary>A day in the month, which only an Interval in months or years allows.</summary>
    public abstract record DayInMonth : PreferredDay
    {
        public override bool Allows(Interval interval) => interval.Unit is IntervalUnit.Month or IntervalUnit.Year;

        protected static DateOnly FirstOf(DateOnly date) => new(date.Year, date.Month, 1);
    }

    public sealed record FirstOfMonth : DayInMonth
    {
        public override DateOnly OnOrAfter(DateOnly date) => date.Day == 1 ? date : FirstOf(date).AddMonths(1);

        public override DateOnly OnOrBefore(DateOnly date) => FirstOf(date);
    }

    public sealed record LastOfMonth : DayInMonth
    {
        public override DateOnly OnOrAfter(DateOnly date) => FirstOf(date).AddMonths(1).AddDays(-1);

        public override DateOnly OnOrBefore(DateOnly date) => date.AddDays(1).Day == 1 ? date : FirstOf(date).AddDays(-1);
    }
}
