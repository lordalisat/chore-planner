namespace Chores.Api.Dtos;

/// <summary>Ticks an Item, or unticks it; either records no Completion.</summary>
public sealed record TickItemRequest(bool Ticked);
