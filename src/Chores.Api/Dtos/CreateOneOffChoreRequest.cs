using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>A new One-off Chore; defaults apply to properties the JSON leaves out.</summary>
/// <param name="DueDate">Without one, the Overview shows the Chore under Anytime.</param>
public sealed record CreateOneOffChoreRequest(
    string Name,
    DateOnly? DueDate = null,
    Importance Importance = Importance.Normal,
    int? Effort = null)
{
    /// <summary>The Chore this asks for, or what is wrong with which field.</summary>
    /// <param name="takenNames">The Names of the other Chores, which this one may not share.</param>
    public Result<OneOffChore> ToChore(DateOnly createdOn, IEnumerable<string> takenNames)
    {
        List<FieldError> errors = [];
        string name = errors.CheckName(Name, takenNames);
        return new Result<OneOffChore>(errors, () => new OneOffChore(name, createdOn)
        {
            DueDate = DueDate,
            Importance = Importance,
            Effort = Effort,
        });
    }
}
