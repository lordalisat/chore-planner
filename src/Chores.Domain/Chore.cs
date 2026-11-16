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

    public void Complete(DateOnly date)
    {
        _completions.Add(CompletionOn(date));
        OnCompletionsChanged();
    }

    public void RemoveCompletion(Completion completion)
    {
        _completions.Remove(completion);
        OnCompletionsChanged();
    }

    /// <summary>Sets the Due Date by hand.</summary>
    public abstract void Postpone(DateOnly date);

    /// <summary>Today is after the Due Date, whatever the Importance.</summary>
    public bool IsOverdue(TimeProvider clock) => clock.Today() > OpenDueDate;

    /// <summary>
    /// Rises from 0 four days before the Due Date to 2 four days after it (1 on the Due Date), times the
    /// Importance weight; null when the Chore has no Due Date.
    /// </summary>
    public double? Urgency(TimeProvider clock) => UnweightedUrgency(clock.Today()) * Importance switch
    {
        Importance.Low => 0.75,
        Importance.High => 1.5,
        _ => 1,
    };

    /// <summary>The Due Date that still has to be met: none once a One-off Chore is finished.</summary>
    protected abstract DateOnly? OpenDueDate { get; }

    protected abstract Completion CompletionOn(DateOnly date);

    protected virtual void OnCompletionsChanged()
    {
    }

    protected virtual double? UnweightedUrgency(DateOnly today) =>
        OpenDueDate is { } due ? Math.Clamp((today.DayNumber - due.DayNumber) * 0.25 + 1, 0, 2) : null;
}
