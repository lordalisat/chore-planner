namespace Chores.Domain;

/// <summary>A Chore that is finished for good once completed.</summary>
public sealed class OneOffChore(string name, DateOnly createdOn) : Chore(name, createdOn)
{
    /// <summary>The Due Date set by hand, if any.</summary>
    public DateOnly? DueOn { get; set; }

    public bool IsFinished => Completions.Count > 0;

    public override void Postpone(DateOnly date) => DueOn = date;

    protected override DateOnly? DueDate(DateOnly today) => IsFinished ? null : DueOn;

    protected override double? Progress(DateOnly today) => IsFinished ? null : DueOn is null ? 0.5 : base.Progress(today);
}
