using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>Sets a Chore's Due Date by hand, until its next Completion.</summary>
/// <param name="DueDate">The Due Date the device saw; when it is no longer the Chore's, someone else acted first.</param>
/// <param name="To">The new Due Date; after today.</param>
public sealed record PostponeChoreRequest(DateOnly? DueDate, DateOnly To)
{
    /// <summary>The Chore postponed, or why the date won't do.</summary>
    public Result<Chore> PostponeOn(Chore chore, DateOnly today)
    {
        List<FieldError> errors = [];
        if (To <= today)
        {
            errors.Add(new FieldError("to", "A Chore can only be postponed to a day after today."));
        }
        return new Result<Chore>(errors, () =>
        {
            chore.Postpone(To);
            return chore;
        });
    }
}
