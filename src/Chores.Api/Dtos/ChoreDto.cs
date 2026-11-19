using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>A Chore as the front end sees it, with its scheduling computed for today.</summary>
/// <param name="Kind">"Recurring" or "OneOff".</param>
/// <param name="PreferredDay">"Monday" to "Sunday", "FirstOfMonth" or "LastOfMonth".</param>
/// <param name="Urgency">How pressing the Chore is today; null without a Due Date.</param>
/// <param name="Dormant">Outside its Season; the Overview hides it.</param>
/// <param name="Finished">A One-off Chore that was completed; the Overview hides it.</param>
public sealed record ChoreDto(
    int Id,
    string Kind,
    string Name,
    Importance Importance,
    int? Effort,
    AreaRefDto? Area,
    IntervalDto? Interval,
    string? PreferredDay,
    SeasonDto? Season,
    DateOnly? DueDate,
    double? Urgency,
    bool Overdue,
    bool Dormant,
    bool Finished,
    DateOnly? LastDone)
{
    /// <param name="idOf">The key of a Chore or Area, which the Domain does not hold.</param>
    public static ChoreDto From(Chore chore, Func<object, int> idOf, TimeProvider clock)
    {
        var recurring = chore as RecurringChore;
        return new ChoreDto(
            idOf(chore),
            recurring is null ? "OneOff" : "Recurring",
            chore.Name,
            chore.Importance,
            chore.Effort,
            chore.Area is null ? null : new AreaRefDto(idOf(chore.Area), chore.Area.Name),
            recurring is null ? null : new IntervalDto(recurring.Interval.Count, recurring.Interval.Unit),
            recurring?.PreferredDay is { } day ? PreferredDayText.ToText(day) : null,
            recurring?.Season is { } season ? new SeasonDto(season.FromMonth, season.ToMonth) : null,
            recurring?.DueDate ?? (chore as OneOffChore)?.DueDate,
            chore.Urgency(clock),
            chore.IsOverdue(clock),
            recurring?.IsDormant(clock) ?? false,
            (chore as OneOffChore)?.IsFinished ?? false,
            chore.LastDone);
    }
}
