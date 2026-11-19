using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>
/// New values for the fields a Chore's kind has, replacing the old ones; the kind itself can't change. A Recurring
/// Chore takes the Interval, Preferred Day and Season, a One-off Chore the Due Date; the others are ignored.
/// </summary>
public sealed record EditChoreRequest(
    string Name,
    IntervalDto? Interval = null,
    string? PreferredDay = null,
    SeasonDto? Season = null,
    DateOnly? DueDate = null,
    Importance Importance = Importance.Normal,
    int? Effort = null)
{
    /// <summary>The Chore with these values, or what is wrong with which field, leaving the Chore as it was.</summary>
    /// <param name="takenNames">The Names of the other Chores, which this one may not share.</param>
    public Result<Chore> ApplyTo(Chore chore, IEnumerable<string> takenNames)
    {
        List<FieldError> errors = [];
        string name = errors.ValidName(Name, takenNames);
        (Interval? interval, PreferredDay? preferredDay, Season? season) = chore is RecurringChore
            ? errors.ValidSchedule(Interval, PreferredDay, Season)
            : default;
        // Result only changes the Chore when nothing was wrong
        return new Result<Chore>(errors, () =>
        {
            chore.Name = name;
            chore.Importance = Importance;
            chore.Effort = Effort;
            switch (chore)
            {
                case RecurringChore recurring:
                    // The old Preferred Day may not allow the new Interval, so it goes first
                    recurring.PreferredDay = null;
                    recurring.Interval = interval!;
                    recurring.PreferredDay = preferredDay;
                    recurring.Season = season;
                    break;
                case OneOffChore oneOff:
                    oneOff.DueDate = DueDate;
                    break;
            }
            return chore;
        });
    }
}
