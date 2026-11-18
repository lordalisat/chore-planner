using System.Diagnostics.CodeAnalysis;
using Chores.Domain;

namespace Chores.Api;

/// <summary>A Preferred Day as text, such as "Tuesday" or "LastOfMonth": the same in the database and in JSON.</summary>
public static class PreferredDayText
{
    /// <summary>Every text a Preferred Day can have, which OpenAPI documents as an enum.</summary>
    public static readonly IReadOnlyList<string> All =
        [.. Enum.GetNames<DayOfWeek>(), nameof(PreferredDay.FirstOfMonth), nameof(PreferredDay.LastOfMonth)];

    public static string ToText(PreferredDay day) => day switch
    {
        PreferredDay.Weekday weekday => weekday.Day.ToString(),
        PreferredDay.FirstOfMonth => nameof(PreferredDay.FirstOfMonth),
        PreferredDay.LastOfMonth => nameof(PreferredDay.LastOfMonth),
        _ => throw new ArgumentOutOfRangeException(nameof(day), day, null),
    };

    public static PreferredDay FromText(string text) =>
        TryFromText(text, out PreferredDay? day) ? day : throw new FormatException($"Not a Preferred Day: {text}");

    public static bool TryFromText(string text, [NotNullWhen(true)] out PreferredDay? day)
    {
        day = text switch
        {
            nameof(PreferredDay.FirstOfMonth) => new PreferredDay.FirstOfMonth(),
            nameof(PreferredDay.LastOfMonth) => new PreferredDay.LastOfMonth(),
            // Exact names only; Enum.TryParse would also take numbers and any casing
            _ when Enum.GetNames<DayOfWeek>().Contains(text) => Enum.Parse<DayOfWeek>(text),
            _ => null,
        };
        return day is not null;
    }
}
