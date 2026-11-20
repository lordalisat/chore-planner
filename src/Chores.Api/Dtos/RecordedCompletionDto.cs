namespace Chores.Api.Dtos;

/// <summary>A Chore after a Completion was recorded, with that Completion's id so it can be undone.</summary>
public sealed record RecordedCompletionDto(int CompletionId, ChoreDto Chore);
