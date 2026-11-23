namespace Chores.Domain;

/// <summary>
/// One of the things doing a Chore involves, such as the mirror when cleaning the bathroom; no schedule of its own,
/// done in any order.
/// </summary>
public sealed class Item
{
    public const int NameMaxLength = 200;

    /// <param name="name">Trimmed; required, at most <see cref="NameMaxLength"/> characters.</param>
    /// <param name="description">As a Chore's Description: trimmed, null when blank, at most 2,000 characters.</param>
    public Item(string name, string? description = null)
    {
        Name = name.Trim();
        if (Name.Length == 0)
        {
            // Messages without a parameter name: the API shows them to the household as they are
            throw new ArgumentException("Every Item needs a name.");
        }
        if (Name.Length > NameMaxLength)
        {
            throw new ArgumentException($"An Item's name holds at most {NameMaxLength} characters.");
        }
        Description = Chore.DescriptionText(description);
    }

    public string Name { get; private set; }
    public string? Description { get; private set; }

    /// <summary>Marked done while working on the Chore; records no Completion.</summary>
    public bool Ticked { get; private set; }

    public void Tick() => Ticked = true;
    public void Untick() => Ticked = false;
}
