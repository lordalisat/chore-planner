namespace Chores.Api.Dtos;

/// <param name="FromMonth">1 to 12, January to December.</param>
/// <param name="ToMonth">1 to 12; before FromMonth when the Season wraps the year.</param>
public sealed record SeasonDto(int FromMonth, int ToMonth);
