namespace Chores.Domain;

/// <summary>
/// One of the things doing a Chore involves, such as the mirror when cleaning the bathroom; no schedule of its own,
/// done in any order.
/// </summary>
public sealed class Item
{
    public const int NameMaxLength = 200;

    /// <param name="name">Trimmed; required, at most <see cref="NameMaxLength"/> characters.</param>
    /// <param name="description">As a Chore's Description, at most <see cref="Chore.DescriptionMaxLength"/> characters.</param>
    public Item(string name, string? description = null) => Change(name, description);

    public string Name { get; private set; } = "";
    public string? Description { get; private set; }

    /// <summary>Marked done while working on the Chore; records no Completion.</summary>
    public bool Ticked { get; private set; }

    /// <summary>Where the Item stands in its Chore's list, which the Chore keeps.</summary>
    internal int Position { get; set; }

    /// <summary>A new Name and Description for the same Item, which keeps its tick.</summary>
    public void Change(string name, string? description)
    {
        string trimmed = name.Trim();
        if (trimmed.Length is 0 or > NameMaxLength)
        {
            throw new ArgumentException($"An Item's Name is 1 to {NameMaxLength} characters.", nameof(name));
        }
        Description = Chore.DescriptionText(description);
        Name = trimmed;
    }

    public void Tick() => Ticked = true;
    public void Untick() => Ticked = false;
}
