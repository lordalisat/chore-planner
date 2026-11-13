namespace Chores.Domain;

/// <summary>A Chore that comes back after an Interval.</summary>
public sealed class RecurringChore(string name, Interval interval, DateOnly createdOn) : Chore(name, createdOn)
{
    public Interval Interval { get; set; } = interval;
    public DayOfWeek? PreferredDay { get; set; }
    public Season? Season { get; set; }

    /// <summary>The Postpone date, overriding the calculated Due Date until the next Completion.</summary>
    public DateOnly? PostponedTo { get; private set; }

    /// <summary>
    /// Last Done when the Chore was postponed: the Postpone holds while Last Done is unchanged, so a back-dated
    /// Completion keeps it and removing the Completion that replaced it brings it back.
    /// </summary>
    public DateOnly? PostponedAfter { get; private set; }

    public override void Postpone(DateOnly date)
    {
        PostponedTo = date;
        PostponedAfter = LastDone;
    }

    /// <summary>Outside its Season: no Due Date, never Overdue, no Urgency.</summary>
    public bool IsDormant(TimeProvider clock) => IsDormant(clock.Today());

    private bool IsDormant(DateOnly today) => Season is { } season && !season.Contains(today);

    protected override DateOnly? DueDate(DateOnly today)
    {
        if (IsDormant(today))
        {
            return null;
        }
        if (PostponedTo is { } postponed && PostponedAfter == LastDone)
        {
            return postponed;
        }
        if (LastDone is not { } lastDone)
        {
            return PreferredDay is { } preferredDay ? NextOnOrAfter(CreatedOn, preferredDay) : CreatedOn;
        }

        DateOnly due = Interval.AddTo(lastDone);
        if (PreferredDay is { } preferred)
        {
            int offset = DaysUntil(due, preferred);
            due = due.AddDays(offset > 3 ? offset - 7 : offset);
            if (due <= lastDone)
            {
                due = NextOnOrAfter(lastDone.AddDays(1), preferred);
            }
        }
        return due;
    }

    private static DateOnly NextOnOrAfter(DateOnly date, DayOfWeek day) => date.AddDays(DaysUntil(date, day));

    private static int DaysUntil(DateOnly date, DayOfWeek day) => ((int)day - (int)date.DayOfWeek + 7) % 7;
}
