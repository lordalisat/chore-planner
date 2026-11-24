namespace Chores.Api.Dtos;

/// <summary>One of a Chore's Items, in the Chore's order.</summary>
/// <param name="Ticked">Marked done while working on the Chore.</param>
public sealed record ItemDto(int Id, string Name, string? Description, bool Ticked);
