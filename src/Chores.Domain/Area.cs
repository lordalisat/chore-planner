namespace Chores.Domain;

/// <summary>A part of the house a Chore belongs to, optionally inside one parent Area.</summary>
public sealed class Area(string name, Area? parent = null)
{
    // For persistence, which sets the Parent after construction
    private Area(string name) : this(name, null)
    {
    }

    public string Name { get; set; } = name;
    public Area? Parent { get; set; } = parent;
}
