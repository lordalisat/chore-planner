using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>A Chore as the front end sees it, with its scheduling computed for today.</summary>
/// <param name="Kind">"Recurring" or "OneOff".</param>
/// <param name="Description">What doing the Chore involves, as plain text with line breaks; null without one.</param>
/// <param name="Items">What doing the Chore involves, one by one, in the household's order.</param>
/// <param name="PreferredDay">"Monday" to "Sunday", "FirstOfMonth", "LastOfMonth", or a weekday in the month such as "FirstSaturday" or "LastFriday".</param>
/// <param name="Urgency">How pressing the Chore is today; null without a Due Date.</param>
/// <param name="Dormant">Outside its Season; the Overview hides it.</param>
/// <param name="Finished">A One-off Chore that was completed; the Overview hides it.</param>
/// <param name="CanSkip">Whether the Chore can move on to its next Due Date without being done: a Recurring one.</param>
/// <param name="CompletionCount">How many Completions, done or skipped, the Chore has; deleting one with any Archives it.</param>
/// <param name="ArchivedOn">The date the Chore was Archived on; null when it is not.</param>
public sealed record ChoreDto(
    int Id,
    string Kind,
    string Name,
    Importance Importance,
    int? Effort,
    string? Description,
    IReadOnlyList<ItemDto> Items,
    AreaRefDto? Area,
    IntervalDto? Interval,
    string? PreferredDay,
    SeasonDto? Season,
    DateOnly? DueDate,
    double? Urgency,
    bool Overdue,
    bool Dormant,
    bool Finished,
    bool CanSkip,
    DateOnly? LastDone,
    int CompletionCount,
    DateOnly? ArchivedOn)
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
            chore.Description,
            [.. chore.Items.Select(item => new ItemDto(idOf(item), item.Name, item.Description, item.Ticked))],
            chore.Area is null ? null : new AreaRefDto(idOf(chore.Area), chore.Area.Name),
            recurring is null ? null : new IntervalDto(recurring.Interval.Count, recurring.Interval.Unit),
            recurring?.PreferredDay is { } day ? PreferredDayText.ToText(day) : null,
            recurring?.Season is { } season ? new SeasonDto(season.FromMonth, season.ToMonth) : null,
            recurring?.DueDate ?? (chore as OneOffChore)?.DueDate,
            chore.Urgency(clock),
            chore.IsOverdue(clock),
            recurring?.IsDormant(clock) ?? false,
            chore.IsFinished,
            chore.CanSkip,
            chore.LastDone,
            chore.Completions.Count,
            chore.ArchivedOn);
    }
}
