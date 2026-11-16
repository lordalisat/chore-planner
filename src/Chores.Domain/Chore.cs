namespace Chores.Domain;

/// <summary>A piece of household work: either a Recurring Chore or a One-off Chore.</summary>
public abstract class Chore(string name, DateOnly createdOn)
{
    private readonly List<Completion> _completions = [];

    public string Name { get; set; } = name;
    public DateOnly CreatedOn { get; } = createdOn;
    public Importance Importance { get; set; } = Importance.Normal;

    /// <summary>The estimated number of minutes the Chore takes.</summary>
    public int? Effort { get; set; }

    public Area? Area { get; set; }
    public IReadOnlyList<Completion> Completions => _completions;

    /// <summary>The date of the latest Completion, if any.</summary>
    public DateOnly? LastDone => _completions.Count == 0 ? null : _completions.Max(c => c.Date);

    public void Complete(DateOnly date) => _completions.Add(new Completion(date));

    public void RemoveCompletion(Completion completion) => _completions.Remove(completion);

    /// <summary>Sets the Due Date by hand.</summary>
    public abstract void Postpone(DateOnly date);

    /// <summary>Follows from the Completions and any Postpone; today plays no part.</summary>
    public abstract DateOnly? DueDate { get; }

    /// <summary>Today is after the Due Date, whatever the Importance.</summary>
    public bool IsOverdue(TimeProvider clock)
    {
        DateOnly today = clock.Today();
        return !IsDormant(today) && today > DueDate;
    }

    /// <summary>Progress towards the Due Date as moved by Importance; null when the Chore has none.</summary>
    public double? Urgency(TimeProvider clock)
    {
        DateOnly today = clock.Today();
        return IsDormant(today) ? null : Progress(today);
    }

    protected virtual bool IsDormant(DateOnly today) => false;

    /// <summary>
    /// How far today is from the cycle start (Last Done, or the creation date) to the Due Date, which counts as
    /// 3 days earlier for High and 3 days later for Low Importance: 1 on that date and above 1 after it.
    /// </summary>
    protected virtual double? Progress(DateOnly today)
    {
        if (DueDate is not { } due)
        {
            return null;
        }
        DateOnly start = LastDone ?? CreatedOn;
        if (due <= start)
        {
            return 1;
        }
        DateOnly moved = due.AddDays(Importance switch
        {
            Importance.Low => 3,
            Importance.High => -3,
            _ => 0,
        });
        DateOnly effectiveDue = moved > start ? moved : start.AddDays(1);
        return (double)(today.DayNumber - start.DayNumber) / (effectiveDue.DayNumber - start.DayNumber);
    }
}
