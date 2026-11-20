namespace Chores.Api.Dtos;

/// <summary>An Area as the Areas screen shows it.</summary>
/// <param name="ChoreCount">How many Chores, Archived ones included, use the Area; deleting it detaches them.</param>
public sealed record AreaDto(int Id, string Name, int? ParentId, int ChoreCount);
