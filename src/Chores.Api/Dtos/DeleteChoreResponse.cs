namespace Chores.Api.Dtos;

/// <summary>What deleting a Chore did.</summary>
/// <param name="Archived">True when the Chore had Completions and was Archived, so it can be restored; false when it was removed for good.</param>
public sealed record DeleteChoreResponse(bool Archived);
