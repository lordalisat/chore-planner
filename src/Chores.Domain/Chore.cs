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

    public DateOnly? DueDate(TimeProvider clock) => DueDate(clock.Today());

    /// <summary>Today is after the Due Date, whatever the Importance.</summary>
    public bool IsOverdue(TimeProvider clock)
    {
        DateOnly today = clock.Today();
        return today > DueDate(today);
    }

    /// <summary>Progress towards the Due Date weighted by Importance; null when the Chore has none.</summary>
    public double? Urgency(TimeProvider clock) => Progress(clock.Today()) * Importance switch
    {
        Importance.Low => 0.75,
        Importance.High => 1.5,
        _ => 1,
    };

    protected abstract DateOnly? DueDate(DateOnly today);

    /// <summary>
    /// How far today is from the cycle start (Last Done, or the creation date) to the Due Date:
    /// 1 on the Due Date and above 1 when Overdue.
    /// </summary>
    protected virtual double? Progress(DateOnly today)
    {
        if (DueDate(today) is not { } due)
        {
            return null;
        }
        DateOnly start = LastDone ?? CreatedOn;
        return due <= start ? 1 : (double)(today.DayNumber - start.DayNumber) / (due.DayNumber - start.DayNumber);
    }
}
