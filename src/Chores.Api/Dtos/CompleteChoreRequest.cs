using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>Marks a Chore done.</summary>
/// <param name="DueDate">The Due Date the device saw; when it is no longer the Chore's, someone else acted first.</param>
/// <param name="Date">When it was done; today when left out.</param>
public sealed record CompleteChoreRequest(DateOnly? DueDate, DateOnly? Date = null)
{
    /// <summary>The Chore with this Completion recorded, or why the date won't do.</summary>
    public Result<Chore> CompleteOn(Chore chore, DateOnly today)
    {
        List<FieldError> errors = [];
        DateOnly date = Date ?? today;
        if (date > today)
        {
            errors.Add(new FieldError("date", "A Chore cannot be done in the future."));
        }
        return new Result<Chore>(errors, () =>
        {
            chore.Complete(date);
            return chore;
        });
    }
}
