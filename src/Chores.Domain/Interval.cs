namespace Chores.Domain;

public enum IntervalUnit
{
    Day,
    Week,
    Month,
    Year,
}

/// <summary>How often a Recurring Chore should roughly be done, such as "every 2 weeks".</summary>
public sealed record Interval
{
    public Interval(int count, IntervalUnit unit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        Count = count;
        Unit = unit;
    }

    public int Count { get; }
    public IntervalUnit Unit { get; }

    public DateOnly AddTo(DateOnly date) => Unit switch
    {
        IntervalUnit.Day => date.AddDays(Count),
        IntervalUnit.Week => date.AddDays(7 * Count),
        // DateOnly.AddMonths clamps to the last day of the month
        IntervalUnit.Month => date.AddMonths(Count),
        IntervalUnit.Year => date.AddMonths(12 * Count),
        _ => throw new ArgumentOutOfRangeException(nameof(Unit), Unit, null),
    };
}
