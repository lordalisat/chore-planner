namespace Chores.Domain;

/// <summary>A piece of household work: either a Recurring Chore or a One-off Chore.</summary>
public abstract class Chore(string name, DateOnly createdOn)
{
    private readonly List<Completion> _completions = [];
    private readonly List<Item> _items = [];

    public const int MaxItems = 20;

    public string Name { get; set; } = name;
    public DateOnly CreatedOn { get; } = createdOn;
    public Importance Importance { get; set; } = Importance.Normal;

    /// <summary>The estimated number of minutes the Chore takes.</summary>
    public int? Effort { get; set; }

    /// <summary>The most characters a Description holds, after trimming.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>
    /// What doing the Chore involves, as plain text with its line breaks; trimmed, and null when blank. At most
    /// <see cref="DescriptionMaxLength"/> characters.
    /// </summary>
    public string? Description
    {
        get;
        set => field = DescriptionText(value);
    }

    /// <summary>A Description as a Chore or Item keeps it: trimmed, null when blank, at most <see cref="DescriptionMaxLength"/>.</summary>
    public static string? DescriptionText(string? value)
    {
        string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return trimmed?.Length > DescriptionMaxLength
            ? throw new ArgumentException($"A Description holds at most {DescriptionMaxLength} characters.")
            : trimmed;
    }

    /// <summary>What doing the Chore involves, in the household's order; ticked in any order.</summary>
    public IReadOnlyList<Item> Items => _items;

    /// <summary>
    /// Replaces the Items, in this order; at most <see cref="MaxItems"/>, no two with the same Name. A kept Item is passed
    /// in again, so its tick stays with it.
    /// </summary>
    public void SetItems(IReadOnlyList<Item> items)
    {
        EnsureAllowed(items);
        _items.Clear();
        _items.AddRange(items);
    }

    /// <summary>Throws when a Chore can't have these Items: too many, or two with the same Name.</summary>
    public static void EnsureAllowed(IReadOnlyList<Item> items)
    {
        if (items.Count > MaxItems)
        {
            throw new ArgumentException($"A Chore holds at most {MaxItems} Items.");
        }
        if (items.GroupBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).FirstOrDefault(names => names.Count() > 1) is { } twice)
        {
            throw new ArgumentException($"Two Items are called {twice.Key}.");
        }
    }

    public Area? Area { get; set; }
    public IReadOnlyList<Completion> Completions => _completions;

    /// <summary>The date the household removed the Chore on, keeping its Completions for history; null when not Archived.</summary>
    public DateOnly? ArchivedOn { get; private set; }

    /// <summary>A One-off Chore once completed; a Recurring Chore never is.</summary>
    public virtual bool IsFinished => false;

    /// <summary>Only a Recurring Chore can move on to its next Due Date without being done.</summary>
    public virtual bool CanSkip => false;

    /// <summary>Has Completions to keep, so deleting it Archives it rather than removing it.</summary>
    public bool HasHistory => _completions.Count > 0;

    /// <summary>The date of the latest Completion that was not skipped, if any.</summary>
    public DateOnly? LastDone => _completions.Where(c => !c.Skipped).Select(c => (DateOnly?)c.Date).Max();

    public void Complete(DateOnly date) => Add(CompletionOn(date));

    /// <summary>
    /// Moves on to the next Due Date as if done on the current one, recording a skipped Completion that leaves
    /// Last Done as it is; only when <see cref="CanSkip"/>.
    /// </summary>
    public void Skip(DateOnly date) => Add(CanSkip
        ? CompletionOn(date) with { Skipped = true }
        : throw new InvalidOperationException("Only a Recurring Chore can be skipped."));

    /// <summary>Removes a Completion, done or skipped.</summary>
    public void RemoveCompletion(Completion completion)
    {
        _completions.Remove(completion);
        OnCompletionsChanged();
    }

    /// <summary>Removes the Chore from the household's list but keeps its Completions.</summary>
    public void Archive(DateOnly date) => ArchivedOn = date;

    /// <summary>Brings an Archived Chore back, scheduled from its history as before.</summary>
    public void Restore() => ArchivedOn = null;

    /// <summary>
    /// Whether the Chore is still open with the given Due Date, or without one when null: not once someone acted on it.
    /// </summary>
    public bool IsDueOn(DateOnly? dueDate) => !IsFinished && NextDueDate == dueDate;

    /// <summary>Sets the Due Date by hand.</summary>
    public abstract void Postpone(DateOnly date);

    /// <summary>Today is after the Due Date, whatever the Importance; never for an Archived Chore.</summary>
    public bool IsOverdue(TimeProvider clock) => ArchivedOn is null && clock.Today() > NextDueDate;

    /// <summary>
    /// Rises from 0 four days before the Due Date to 2 four days after it (1 on the Due Date), times the
    /// Importance weight; null when the Chore has no Due Date or is Archived.
    /// </summary>
    public double? Urgency(TimeProvider clock) => ArchivedOn is not null ? null : UnweightedUrgency(clock.Today()) * Importance switch
    {
        Importance.Low => 0.75,
        Importance.High => 1.5,
        _ => 1,
    };

    /// <summary>The Due Date still to be met, which Overdue and Urgency use: none once a One-off Chore is finished.</summary>
    protected abstract DateOnly? NextDueDate { get; }

    protected abstract Completion CompletionOn(DateOnly date);

    protected void Add(Completion completion)
    {
        _completions.Add(completion);
        OnRecorded();
        OnCompletionsChanged();
    }

    /// <summary>After a Completion, done or skipped, is recorded; not when one is undone.</summary>
    protected virtual void OnRecorded()
    {
    }

    protected virtual void OnCompletionsChanged()
    {
    }

    protected virtual double? UnweightedUrgency(DateOnly today) =>
        NextDueDate is { } due ? Math.Clamp((today.DayNumber - due.DayNumber) * 0.25 + 1, 0, 2) : null;
}
