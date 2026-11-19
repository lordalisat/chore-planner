using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>Lets a Recurring Chore move on to its next Due Date without doing it.</summary>
/// <param name="DueDate">The Due Date the device saw; when it is no longer the Chore's, someone else acted first.</param>
public sealed record SkipChoreRequest(DateOnly? DueDate)
{
    /// <summary>The Chore with a skipped Completion recorded today, or why it can't be skipped.</summary>
    public Result<Chore> SkipOn(Chore chore, DateOnly today)
    {
        List<FieldError> errors = [];
        if (chore is not RecurringChore)
        {
            errors.Add(new FieldError("kind", "Only a Recurring Chore can be skipped."));
        }
        return new Result<Chore>(errors, () =>
        {
            ((RecurringChore)chore).Skip(today);
            return chore;
        });
    }
}
