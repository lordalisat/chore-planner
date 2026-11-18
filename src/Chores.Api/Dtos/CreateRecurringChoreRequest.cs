using Chores.Domain;

namespace Chores.Api.Dtos;

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
