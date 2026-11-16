namespace Chores.Domain;

/// <summary>A record that a Chore was done on a given date.</summary>
/// <param name="DueDate">The Due Date it was done against, before any Postpone.</param>
/// <param name="PostponedTo">The Postpone date, when one was in effect.</param>
public sealed record Completion(DateOnly Date, DateOnly? DueDate, DateOnly? PostponedTo);
