namespace Chores.Domain;

/// <summary>A record that a Chore was done on a given date.</summary>
public sealed record Completion(DateOnly Date);
