namespace Chores.Domain;

/// <summary>Which of a weekday's days in a month: the first to the fourth, which every month has, or the last.</summary>
public enum WeekOfMonth
{
    First = 1,
    Second,
    Third,
    Fourth,
    Last,
}
