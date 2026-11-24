using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>Ticks an Item, or unticks it; either records no Completion.</summary>
public sealed record TickItemRequest(bool Ticked)
{
    public void ApplyTo(Item item)
    {
        if (Ticked)
        {
            item.Tick();
        }
        else
        {
            item.Untick();
        }
    }
}
