using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>Checks the fields several requests share, adding at each field what is wrong with it.</summary>
public static class FieldChecks
{
    /// <summary>The trimmed Name, or adds the message at "name" when there is none.</summary>
    public static string CheckRequiredName(this List<FieldError> errors, string? name, string missingMessage)
    {
        // Missing when the JSON leaves it out, whatever its type says
        string trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            errors.Add(new FieldError("name", missingMessage));
        }
        return trimmed;
    }

    /// <summary>The trimmed Name, or adds at "name" why a Chore cannot have it.</summary>
    /// <param name="takenNames">The Names of the other Chores, which this one may not share.</param>
    public static string CheckName(this List<FieldError> errors, string? name, IEnumerable<string> takenNames)
    {
        string trimmed = errors.CheckRequiredName(name, "A Chore needs a name.");
        // A Name tells Chores apart on the Overview
        if (trimmed.Length > 0
            && takenNames.FirstOrDefault(taken => string.Equals(taken.Trim(), trimmed, StringComparison.CurrentCultureIgnoreCase)) is { } taken)
        {
            errors.Add(new FieldError("name", $"There is already a Chore called {taken}."));
        }
        return trimmed;
    }

    /// <summary>Adds at "description" when the Description is longer than a Chore holds.</summary>
    public static void CheckDescription(this List<FieldError> errors, string? description)
    {
        if (description?.Trim().Length > Chore.DescriptionMaxLength)
        {
            errors.Add(new FieldError("description", $"A Description holds at most {Chore.DescriptionMaxLength} characters."));
        }
    }

    /// <summary>
    /// How to build the Items a request gives, in its order, or null and what is wrong at "items". An Item given with the
    /// id of one the Chore has changes that one, so it stays the same Item with its tick; nothing changes until built.
    /// </summary>
    /// <param name="existing">The Chore's Item with an id, if it has one.</param>
    public static List<Func<Item>>? CheckItems(this List<FieldError> errors, IReadOnlyList<ItemRequest>? requested, Func<int, Item?> existing)
    {
        List<ItemRequest> given = [.. requested ?? []];
        // Missing when the JSON leaves the name out, whatever its type says
        List<string> names = [.. given.Select(item => item.Name?.Trim() ?? "")];
        string? problem =
            given.Count > Chore.MaxItems ? $"A Chore holds at most {Chore.MaxItems} Items."
            : names.Any(name => name.Length == 0) ? "Every Item needs a name."
            : names.FirstOrDefault(name => name.Length > Item.NameMaxLength) is not null ? $"An Item's name holds at most {Item.NameMaxLength} characters."
            : given.Any(item => item.Description?.Trim().Length > Chore.DescriptionMaxLength) ? $"An Item's Description holds at most {Chore.DescriptionMaxLength} characters."
            : names.GroupBy(name => name, StringComparer.CurrentCultureIgnoreCase).FirstOrDefault(same => same.Count() > 1) is { } twice ? $"Two Items are called {twice.Key}."
            : null;
        if (problem is not null)
        {
            errors.Add(new FieldError("items", problem));
            return null;
        }
        return [.. given.Select(item => (Func<Item>)(() =>
        {
            if (item.Id is { } id && existing(id) is { } kept)
            {
                kept.Change(item.Name, item.Description);
                return kept;
            }
            return new Item(item.Name, item.Description);
        }))];
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
    /// The Interval, Preferred Day and Season of a Recurring Chore, each null when missing or wrong; the Domain
    /// decides which combinations are valid.
    /// </summary>
    public static (Interval? Interval, PreferredDay? PreferredDay, Season? Season) CheckSchedule(
        this List<FieldError> errors, IntervalDto? intervalDto, string? preferredDayText, SeasonDto? seasonDto)
    {
        Interval? interval = errors.Try("interval", "Every needs a whole number of at least 1.", () => intervalDto is { } given
            ? new Interval(given.Count, given.Unit)
            : throw new ArgumentNullException(nameof(intervalDto)));
        PreferredDay? preferredDay = null;
        if (preferredDayText is not null && !PreferredDayText.TryFromText(preferredDayText, out preferredDay))
        {
            errors.Add(new FieldError("preferredDay", "That is not a day."));
        }
        else if (interval is not null && preferredDay?.Allows(interval) == false)
        {
            errors.Add(new FieldError("preferredDay", preferredDay is PreferredDay.Weekday
                ? "A weekday needs an Interval in weeks, months or years."
                : "The first or last of the month needs an Interval in months or years."));
        }
        Season? season = seasonDto is null
            ? null
            : errors.Try("season", "A Season runs between months 1 and 12.", () => new Season(seasonDto.FromMonth, seasonDto.ToMonth));
        return (interval, preferredDay, season);
    }
}
