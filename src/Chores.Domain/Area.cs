namespace Chores.Domain;

/// <summary>A part of the house a Chore belongs to, optionally inside one parent Area.</summary>
public sealed class Area
{
    private Area? _parent;

    public Area(string name, Area? parent = null)
    {
        Name = name;
        Parent = parent;
    }

    // For persistence, which sets the Parent's field after construction
    private Area(string name) : this(name, null)
    {
    }

    public string Name { get; set; }

    /// <summary>The Area this one sits inside; never this Area itself or one inside it.</summary>
    public Area? Parent
    {
        get => _parent;
        set => _parent = CanSitInside(value) ? value : throw new ArgumentException("An Area cannot sit inside itself.", nameof(value));
    }

    /// <summary>Whether this Area can have the given parent: not when that is this Area or one inside it.</summary>
    public bool CanSitInside(Area? parent)
    {
        for (Area? ancestor = parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor == this)
            {
                return false;
            }
        }
        return true;
    }
}
