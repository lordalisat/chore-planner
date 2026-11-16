namespace Chores.Domain;

/// <summary>A Chore that is finished for good once completed.</summary>
public sealed class OneOffChore(string name, DateOnly createdOn) : Chore(name, createdOn)
{
    /// <summary>Optional, set by hand.</summary>
    public DateOnly? DueDate { get; set; }

    public bool IsFinished => Completions.Count > 0;

    public override void Postpone(DateOnly date) => DueDate = date;

    protected override DateOnly? OpenDueDate => IsFinished ? null : DueDate;

    protected override Completion CompletionOn(DateOnly date) => new(date, DueDate, null);

    /// <summary>Without a Due Date it counts as two days before due.</summary>
    protected override double? UnweightedUrgency(DateOnly today) => DueDate is null && !IsFinished ? 0.5 : base.UnweightedUrgency(today);
}
