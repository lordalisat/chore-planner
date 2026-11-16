namespace Chores.Domain;

/// <summary>A Chore that comes back after an Interval.</summary>
public sealed class RecurringChore : Chore
{
    private readonly List<DateOnly> _skippedDueDates = [];

    public RecurringChore(string name, Interval interval, DateOnly createdOn) : base(name, createdOn)
    {
        Interval = interval;
    }

    public Interval Interval
    {
        get;
        set
        {
            field = value;
            Reschedule();
        }
    }

    public DayOfWeek? PreferredDay
    {
        get;
        set
        {
            field = value;
            Reschedule();
        }
    }

    public Season? Season
    {
        get;
        set
        {
            field = value;
            Reschedule();
        }
    }

    /// <summary>Stored so it can be queried; kept up to date by every change that affects it.</summary>
    public DateOnly DueDate { get; private set; }

    /// <summary>The Postpone date, overriding the calculated Due Date until the next Completion or Skip.</summary>
    public DateOnly? PostponedTo { get; private set; }

    public IReadOnlyList<DateOnly> SkippedDueDates => _skippedDueDates;

    public override void Postpone(DateOnly date)
    {
        PostponedTo = date;
        Reschedule();
    }

    /// <summary>Moves on to the next Due Date as if done on the current one, without a Completion.</summary>
    public void Skip()
    {
        _skippedDueDates.Add(DueDate);
        PostponedTo = null;
        Reschedule();
    }

    public void RemoveSkip(DateOnly skippedDueDate)
    {
        _skippedDueDates.Remove(skippedDueDate);
        PostponedTo = null;
        Reschedule();
    }

    /// <summary>Outside its Season; occurrences that would fall there are skipped by the Due Date.</summary>
    public bool IsDormant(TimeProvider clock) => Season is { } season && !season.Contains(clock.Today());

    protected override DateOnly? OpenDueDate => DueDate;

    protected override Completion CompletionOn(DateOnly date) => new(date, CalculatedDueDate(), PostponedTo);

    protected override void OnCompletionsChanged()
    {
        PostponedTo = null;
        Reschedule();
    }

    private void Reschedule() => DueDate = PostponedTo ?? CalculatedDueDate();

    private DateOnly CalculatedDueDate()
    {
        // Last Done or the latest skipped Due Date, whichever is later
        if (_skippedDueDates.Select(d => (DateOnly?)d).Append(LastDone).Max() is not { } from)
        {
            DateOnly first = FirstPreferredDayFrom(CreatedOn);
            return Season is { } dormantSeason && !dormantSeason.Contains(first)
                ? FirstPreferredDayFrom(dormantSeason.NextStartAfter(first))
                : first;
        }

        DateOnly due = DueAfter(from);
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
            int offset = DaysUntil(due, preferred);
            due = due.AddDays(offset > 3 ? offset - 7 : offset);
            if (due <= lastDone)
            {
                due = NextOnOrAfter(lastDone.AddDays(1), preferred);
            }
        }
        return due;
    }

    private DateOnly FirstPreferredDayFrom(DateOnly date) => PreferredDay is { } preferred ? NextOnOrAfter(date, preferred) : date;

    private static DateOnly NextOnOrAfter(DateOnly date, DayOfWeek day) => date.AddDays(DaysUntil(date, day));

    private static int DaysUntil(DateOnly date, DayOfWeek day) => ((int)day - (int)date.DayOfWeek + 7) % 7;
}
