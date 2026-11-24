using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>
/// New values for the fields a Chore's kind has, replacing the old ones; the kind itself can't change. A Recurring
/// Chore takes the Interval, Preferred Day and Season, a One-off Chore the Due Date; the others are ignored.
/// </summary>
/// <param name="Items">The Items in their new order; left out, the Chore keeps the ones it has.</param>
public sealed record EditChoreRequest(
    string Name,
    IntervalDto? Interval = null,
    string? PreferredDay = null,
    SeasonDto? Season = null,
    DateOnly? DueDate = null,
    Importance Importance = Importance.Normal,
    int? Effort = null,
    int? AreaId = null,
    string? Description = null,
    IReadOnlyList<ItemRequest>? Items = null)
{
    /// <summary>The Chore with these values, or what is wrong with which field, leaving the Chore as it was.</summary>
    /// <param name="takenNames">The Names of the other Chores, which this one may not share.</param>
    /// <param name="area">The Area <see cref="AreaId"/> names; null when there is no such Area.</param>
    /// <param name="idOf">The key of one of the Chore's Items, which the Domain does not hold.</param>
    public Result<Chore> ApplyTo(Chore chore, IEnumerable<string> takenNames, Area? area, Func<Item, int> idOf)
    {
        List<FieldError> errors = [];
        string name = errors.CheckName(Name, takenNames);
        errors.CheckArea(AreaId, area);
        errors.CheckDescription(Description);
        List<Func<Item>>? items = Items is null ? null : errors.CheckItems(Items, id => chore.Items.FirstOrDefault(item => idOf(item) == id));
        (Interval? interval, PreferredDay? preferredDay, Season? season) = chore is RecurringChore
            ? errors.CheckSchedule(Interval, PreferredDay, Season)
            : default;
        // Result only changes the Chore when nothing was wrong
        return new Result<Chore>(errors, () =>
        {
            chore.Name = name;
            chore.Importance = Importance;
            chore.Effort = Effort;
            chore.Area = area;
            chore.Description = Description;
            if (items is not null)
            {
                chore.SetItems([.. items.Select(build => build())]);
            }
            // Which fields apply depends on the kind; the Domain has no use for a request's spare ones
            switch (chore)
            {
                case RecurringChore recurring:
                    recurring.Reschedule(interval!, preferredDay, season);
                    break;
                case OneOffChore oneOff:
                    oneOff.DueDate = DueDate;
                    break;
            }
            return chore;
        });
    }
}
