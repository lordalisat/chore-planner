namespace Chores.Domain;

/// <summary>The inclusive range of months in which a Recurring Chore applies; may wrap the year, such as November to February.</summary>
public sealed record Season
{
    public Season(int fromMonth, int toMonth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(fromMonth, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fromMonth, 12);
        ArgumentOutOfRangeException.ThrowIfLessThan(toMonth, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(toMonth, 12);
        FromMonth = fromMonth;
        ToMonth = toMonth;
    }

    public int FromMonth { get; }
    public int ToMonth { get; }

    public bool Contains(DateOnly date) => FromMonth <= ToMonth
        ? date.Month >= FromMonth && date.Month <= ToMonth
        : date.Month >= FromMonth || date.Month <= ToMonth;
}
