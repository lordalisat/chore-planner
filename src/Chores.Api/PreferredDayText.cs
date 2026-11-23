using System.Diagnostics.CodeAnalysis;
using Chores.Domain;

namespace Chores.Api;

/// <summary>
/// A Preferred Day as text, such as "Tuesday", "LastOfMonth" or "FirstSaturday": the same in the database and in JSON.
/// </summary>
public static class PreferredDayText
{
    private static readonly IReadOnlyList<(string Text, PreferredDay Day)> Days =
    [
        .. Enum.GetValues<DayOfWeek>().Select(day => (day.ToString(), (PreferredDay)day)),
        (nameof(PreferredDay.FirstOfMonth), new PreferredDay.FirstOfMonth()),
        (nameof(PreferredDay.LastOfMonth), new PreferredDay.LastOfMonth()),
        .. Enum.GetValues<WeekOfMonth>().SelectMany(week => Enum.GetValues<DayOfWeek>().Select(day =>
            ($"{week}{day}", (PreferredDay)new PreferredDay.WeekdayInMonth(week, day)))),
    ];

    /// <summary>Every text a Preferred Day can have, which OpenAPI documents as an enum.</summary>
    public static readonly IReadOnlyList<string> All = [.. Days.Select(entry => entry.Text)];

    public static string ToText(PreferredDay day) =>
        Days.FirstOrDefault(entry => entry.Day == day).Text ?? throw new ArgumentOutOfRangeException(nameof(day), day, null);

    public static PreferredDay FromText(string text) =>
        TryFromText(text, out PreferredDay? day) ? day : throw new FormatException($"Not a Preferred Day: {text}");

    // Exact texts only: no numbers, no other casing
    public static bool TryFromText(string text, [NotNullWhen(true)] out PreferredDay? day)
    {
        day = Days.FirstOrDefault(entry => entry.Text == text).Day;
        return day is not null;
    }
}
