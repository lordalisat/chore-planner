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

    /// <summary>The Postpone date, overriding the calculated Due Date until the next Completion, done or skipped.</summary>
    public DateOnly? PostponedTo { get; private set; }

    /// <summary>Follows from the Completions and any Postpone; today plays no part.</summary>
    public DateOnly DueDate => PostponedTo ?? CalculatedDueDate();

    public override void Postpone(DateOnly date) => PostponedTo = date;

    /// <summary>
    /// Moves on to the next Due Date as if done on the current one, recording a skipped Completion that leaves
    /// Last Done as it is.
    /// </summary>
    public void Skip(DateOnly date) => Add(new Completion(date, CalculatedDueDate(), PostponedTo, Skipped: true));

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

    /// <summary>Outside its Season; occurrences that would fall there are skipped by the Due Date.</summary>
    public bool IsDormant(TimeProvider clock) => Season is { } season && !season.Contains(clock.Today());

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
        if (PreferredDay is { } preferred)
        {
            due = preferred.NearestTo(due);
            if (due <= lastDone)
            {
                due = preferred.OnOrAfter(lastDone.AddDays(1));
            }
        }
        return due;
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
