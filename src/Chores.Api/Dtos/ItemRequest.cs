namespace Chores.Api.Dtos;

/// <summary>An Item as a request gives it, in the Chore's order.</summary>
/// <param name="Id">An Item the Chore has, which keeps its tick; null for a new one.</param>
public sealed record ItemRequest(int? Id, string Name, string? Description = null);
