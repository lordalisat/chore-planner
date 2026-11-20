namespace Chores.Domain;

/// <summary>A Chore that comes back after an Interval.</summary>
public sealed class RecurringChore(string name, Interval interval, DateOnly createdOn) : Chore(name, createdOn)
{
    // For persistence, which sets the Interval and Preferred Day through their backing fields, past the setters' check
    private RecurringChore(string name, DateOnly createdOn) : this(name, null!, createdOn)
    {
    }

    public Interval Interval
    {
        get;
        set
        {
            EnsureAllowed(value, PreferredDay);
            field = value;
        }
    } = interval;

    /// <summary>A weekday, or the first or last of the month for an Interval in months or years.</summary>
    public PreferredDay? PreferredDay
    {
        get;
        set
        {
            EnsureAllowed(Interval, value);
            field = value;
        }
    }

    public Season? Season { get; set; }

    public override bool CanSkip => true;

    /// <summary>The Postpone date, overriding the calculated Due Date until the next Completion, done or skipped.</summary>
    public DateOnly? PostponedTo { get; private set; }

    /// <summary>Follows from the Completions and any Postpone; today plays no part.</summary>
    public DateOnly DueDate => PostponedTo ?? CalculatedDueDate();

    public override void Postpone(DateOnly date) => PostponedTo = date;

    /// <summary>Replaces the Interval, Preferred Day and Season at once, so the new Interval is checked against the new Preferred Day only.</summary>
    public void Reschedule(Interval interval, PreferredDay? preferredDay, Season? season)
    {
        EnsureAllowed(interval, preferredDay);
        // The old Preferred Day may not allow the new Interval, so it goes first
        PreferredDay = null;
        Interval = interval;
        PreferredDay = preferredDay;
        Season = season;
    }

    /// <summary>
    /// The expected occurrences after the Due Date, up to and including <paramref name="until"/>, assuming each
    /// is done exactly on its Due Date.
    /// </summary>
    public IEnumerable<DateOnly> Forecasts(DateOnly until)
    {
        for (DateOnly next = NextOccurrenceAfter(DueDate); next <= until; next = NextOccurrenceAfter(next))
        {
            yield return next;
        }
    }

    /// <summary>
    /// Outside its Season, waiting for a Due Date inside the next one; occurrences that would fall outside it are skipped
    /// by the Due Date. A Due Date missed inside the Season, or Postponed outside it, keeps the Chore showing.
    /// </summary>
    public bool IsDormant(TimeProvider clock) =>
        Season is { } season && !season.Contains(clock.Today()) && DueDate > clock.Today() && season.Contains(DueDate);

    protected override DateOnly? NextDueDate => DueDate;

    protected override Completion CompletionOn(DateOnly date) => new(date, CalculatedDueDate(), PostponedTo, Skipped: false);

    protected override void OnCompletionsChanged() => PostponedTo = null;

    private DateOnly CalculatedDueDate()
    {
        // The latest Completion moves the schedule on: a done one from its date, a skipped one from the Due Date it skipped
        if (Completions.Select(c => c.Skipped ? c.PostponedTo ?? c.DueDate : c.Date).Max() is { } from)
        {
            return NextOccurrenceAfter(from);
        }
        DateOnly first = FirstPreferredDayFrom(CreatedOn);
        return Season is { } season && !season.Contains(first) ? FirstPreferredDayFrom(season.NextStartAfter(first)) : first;
    }

    /// <summary>The occurrence after one done on the given date, skipping any that fall outside the Season.</summary>
    private DateOnly NextOccurrenceAfter(DateOnly done)
    {
        DateOnly due = DueAfter(done);
        if (Season is { } season && !season.Contains(due))
        {
            // Follow the schedule to its first occurrence inside the next Season
            DateOnly seasonStart = season.NextStartAfter(due);
            while (due < seasonStart)
            {
                due = DueAfter(due);
            }
            if (!season.Contains(due))
            {
                due = FirstPreferredDayFrom(seasonStart);
            }
        }
        return due;
    }

    private DateOnly DueAfter(DateOnly lastDone)
    {
        DateOnly due = Interval.AddTo(lastDone);
        // Every Preferred Day needs an Interval long enough that its nearest one still falls after Last Done
        return PreferredDay is { } preferred ? preferred.NearestTo(due) : due;
    }

    private static void EnsureAllowed(Interval interval, PreferredDay? preferredDay)
    {
        if (preferredDay?.Allows(interval) == false)
        {
            throw new ArgumentException("This Preferred Day does not allow that Interval.");
        }
    }

    private DateOnly FirstPreferredDayFrom(DateOnly date) => PreferredDay is { } preferred ? preferred.OnOrAfter(date) : date;
}
