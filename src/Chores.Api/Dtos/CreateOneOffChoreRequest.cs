using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>A new One-off Chore; defaults apply to properties the JSON leaves out.</summary>
/// <param name="DueDate">Without one, the Overview shows the Chore under Anytime.</param>
public sealed record CreateOneOffChoreRequest(
    string Name,
    DateOnly? DueDate = null,
    Importance Importance = Importance.Normal,
    int? Effort = null,
    int? AreaId = null,
    string? Description = null,
    IReadOnlyList<ItemRequest>? Items = null)
{
    /// <summary>The Chore this asks for, or what is wrong with which field.</summary>
    /// <param name="takenNames">The Names of the other Chores, which this one may not share.</param>
    /// <param name="area">The Area <see cref="AreaId"/> names; null when there is no such Area.</param>
    public Result<OneOffChore> ToChore(DateOnly createdOn, IEnumerable<string> takenNames, Area? area)
    {
        List<FieldError> errors = [];
        string name = errors.CheckName(Name, takenNames);
        errors.CheckArea(AreaId, area);
        errors.CheckDescription(Description);
        List<Item>? items = errors.CheckItems(Items, _ => null);
        return new Result<OneOffChore>(errors, () =>
        {
            var chore = new OneOffChore(name, createdOn)
            {
                DueDate = DueDate,
                Importance = Importance,
                Effort = Effort,
                Area = area,
                Description = Description,
            };
            chore.SetItems(items!);
            return chore;
        });
    }
}
