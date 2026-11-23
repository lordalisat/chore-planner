namespace Chores.Domain;

/// <summary>
/// Which of a weekday's days in a month: the first to the fourth, which every month has, or the last. A closed set,
/// stored as part of a Preferred Day's text ("FirstSaturday"), never as a number; the numbers count weeks from the first.
/// </summary>
public enum Ordinal
{
    First = 1,
    Second,
    Third,
    Fourth,
    Last,
}
