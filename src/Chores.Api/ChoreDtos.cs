using Chores.Domain;

namespace Chores.Api;

/// <summary>A Chore as the front end sees it, with its scheduling computed for today.</summary>
/// <param name="Kind">"Recurring" or "OneOff".</param>
/// <param name="PreferredDay">"Monday" to "Sunday", "FirstOfMonth" or "LastOfMonth".</param>
/// <param name="Urgency">How pressing the Chore is today; null without a Due Date.</param>
/// <param name="Dormant">Outside its Season; the Overview hides it.</param>
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
            chore.LastDone);
    }
}

public sealed record IntervalDto(int Count, IntervalUnit Unit);

/// <param name="FromMonth">1 to 12, January to December.</param>
/// <param name="ToMonth">1 to 12; before FromMonth when the Season wraps the year.</param>
public sealed record SeasonDto(int FromMonth, int ToMonth);

public sealed record AreaRefDto(int Id, string Name);

/// <summary>A new Recurring Chore; defaults apply to properties the JSON leaves out.</summary>
/// <param name="LastDone">When moving over from elsewhere: the Due Date then follows from it, not from today.</param>
public sealed record CreateRecurringChoreRequest(
    string Name,
    IntervalDto Interval,
    string? PreferredDay = null,
    SeasonDto? Season = null,
    Importance Importance = Importance.Normal,
    int? Effort = null,
    DateOnly? LastDone = null)
{
    /// <summary>
    /// The Chore this asks for, or null with what is wrong in <paramref name="errors"/>. The Domain decides what is
    /// valid; each rejection is reported at the field the form shows it on.
    /// </summary>
    /// <param name="takenNames">The Names of the other Chores, which this one may not share.</param>
    public RecurringChore? ToChore(DateOnly createdOn, IEnumerable<string> takenNames, FieldErrors errors)
    {
        // Missing when the JSON leaves it out, whatever its type says
        string name = Name?.Trim() ?? "";
        if (name.Length == 0)
        {
            errors.Add("name", "A Chore needs a name.");
        }
        // A Name tells Chores apart on the Overview
        else if (takenNames.FirstOrDefault(taken => string.Equals(taken.Trim(), name, StringComparison.CurrentCultureIgnoreCase)) is { } taken)
        {
            errors.Add("name", $"There is already a Chore called {taken}.");
        }
        Interval? interval = errors.Try("interval", "Every needs a whole number of at least 1.", () => Interval is { } given
            ? new Interval(given.Count, given.Unit)
            : throw new ArgumentNullException(nameof(Interval)));
        PreferredDay? preferredDay = null;
        if (PreferredDay is not null && !PreferredDayText.TryFromText(PreferredDay, out preferredDay))
        {
            errors.Add("preferredDay", "That is not a day.");
        }
        else if (interval is not null && preferredDay?.Allows(interval) == false)
        {
            errors.Add("preferredDay", "The first or last of the month needs an Interval in months or years.");
        }
        Season? season = Season is null
            ? null
            : errors.Try("season", "A Season runs between months 1 and 12.", () => new Season(Season.FromMonth, Season.ToMonth));
        if (errors.Any)
        {
            return null;
        }

        var chore = new RecurringChore(name, interval!, createdOn)
        {
            PreferredDay = preferredDay,
            Season = season,
            Importance = Importance,
            Effort = Effort,
        };
        if (LastDone is { } lastDone)
        {
            chore.Complete(lastDone);
        }
        return chore;
    }
}
