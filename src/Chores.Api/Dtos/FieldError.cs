using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>What is wrong with one field of a request, keyed by its JSON name so the form shows it at that field.</summary>
public sealed record FieldError(string Field, string Message);

public static class FieldErrorList
{
    /// <summary>Builds a Domain value, or adds the message at the field when the Domain rejects it.</summary>
    public static T? Try<T>(this List<FieldError> errors, string field, string message, Func<T> build) where T : class
    {
        try
        {
            return build();
        }
        catch (ArgumentException)
        {
            errors.Add(new FieldError(field, message));
            return null;
        }
    }

    /// <summary>The trimmed Name, or adds at "name" why a Chore cannot have it.</summary>
    /// <param name="takenNames">The Names of the other Chores, which this one may not share.</param>
    public static string CheckName(this List<FieldError> errors, string? name, IEnumerable<string> takenNames)
    {
        // Missing when the JSON leaves it out, whatever its type says
        string trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            errors.Add(new FieldError("name", "A Chore needs a name."));
        }
        // A Name tells Chores apart on the Overview
        else if (takenNames.FirstOrDefault(taken => string.Equals(taken.Trim(), trimmed, StringComparison.CurrentCultureIgnoreCase)) is { } taken)
        {
            errors.Add(new FieldError("name", $"There is already a Chore called {taken}."));
        }
        return trimmed;
    }

    /// <summary>Adds at "areaId" when the request names an Area that was not found.</summary>
    /// <param name="area">The Area found for <paramref name="areaId"/>, if any.</param>
    public static void CheckArea(this List<FieldError> errors, int? areaId, Area? area)
    {
        if (areaId is not null && area is null)
        {
            errors.Add(new FieldError("areaId", "That Area does not exist."));
        }
    }

    /// <summary>
    /// The Interval, Preferred Day and Season of a Recurring Chore; the Domain decides which combinations are valid.
    /// </summary>
    public static (Interval? Interval, PreferredDay? PreferredDay, Season? Season) CheckSchedule(
        this List<FieldError> errors, IntervalDto? interval, string? preferredDay, SeasonDto? season)
    {
        Interval? validInterval = errors.Try("interval", "Every needs a whole number of at least 1.", () => interval is { } given
            ? new Interval(given.Count, given.Unit)
            : throw new ArgumentNullException(nameof(interval)));
        PreferredDay? validDay = null;
        if (preferredDay is not null && !PreferredDayText.TryFromText(preferredDay, out validDay))
        {
            errors.Add(new FieldError("preferredDay", "That is not a day."));
        }
        else if (validInterval is not null && validDay?.Allows(validInterval) == false)
        {
            errors.Add(new FieldError("preferredDay", validDay is PreferredDay.Weekday
                ? "A weekday needs an Interval in weeks, months or years."
                : "The first or last of the month needs an Interval in months or years."));
        }
        Season? validSeason = season is null
            ? null
            : errors.Try("season", "A Season runs between months 1 and 12.", () => new Season(season.FromMonth, season.ToMonth));
        return (validInterval, validDay, validSeason);
    }
}
