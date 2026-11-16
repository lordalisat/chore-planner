namespace Chores.Domain;

/// <summary>A Chore that comes back after an Interval.</summary>
public sealed class RecurringChore(string name, Interval interval, DateOnly createdOn) : Chore(name, createdOn)
{
    public Interval Interval { get; set; } = interval;
    public DayOfWeek? PreferredDay { get; set; }
    public Season? Season { get; set; }

    private readonly List<DateOnly> _skippedDueDates = [];

    public IReadOnlyList<DateOnly> SkippedDueDates => _skippedDueDates;

    /// <summary>The Postpone date, overriding the calculated Due Date until the next Completion or Skip.</summary>
    public DateOnly? PostponedTo { get; private set; }

    /// <summary>
    /// Where the schedule moved on from when the Chore was postponed: the Postpone holds while that is unchanged, so
    /// a back-dated Completion keeps it and removing the Completion or Skip that replaced it brings it back.
    /// </summary>
    public DateOnly? PostponedAfter { get; private set; }

    /// <summary>Last Done or the latest skipped Due Date, whichever is later.</summary>
    private DateOnly? ScheduledFrom => _skippedDueDates.Select(d => (DateOnly?)d).Append(LastDone).Max();

    public override void Postpone(DateOnly date)
    {
        PostponedTo = date;
        PostponedAfter = ScheduledFrom;
    }

    /// <summary>Moves on to the next Due Date as if done on the current one, without a Completion.</summary>
    public void Skip() => _skippedDueDates.Add(DueDate!.Value);

    public void RemoveSkip(DateOnly skippedDueDate) => _skippedDueDates.Remove(skippedDueDate);

    /// <summary>Outside its Season; occurrences that would fall there are skipped by the Due Date.</summary>
    public bool IsDormant(TimeProvider clock) => Season is { } season && !season.Contains(clock.Today());

    public override DateOnly? DueDate
    {
        get
        {
            if (PostponedTo is { } postponed && PostponedAfter == ScheduledFrom)
            {
                return postponed;
            }
            if (ScheduledFrom is not { } from)
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
