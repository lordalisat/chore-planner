namespace Chores.Domain;

/// <summary>A record that a Chore's Due Date was dealt with on a given date: done, or skipped.</summary>
/// <param name="DueDate">The Due Date it dealt with, before any Postpone.</param>
/// <param name="PostponedTo">The Postpone date, when one was in effect.</param>
/// <param name="Skipped">Moved on without doing the Chore; it does not count towards Last Done.</param>
public sealed record Completion(DateOnly Date, DateOnly? DueDate, DateOnly? PostponedTo, bool Skipped);
