using Chores.Domain;

namespace Chores.Api.Dtos;

/// <summary>A new Area, or new values for one; without a parent the Area sits at the top.</summary>
public sealed record SaveAreaRequest(string Name, int? ParentId = null)
{
    /// <summary>The given Area with these values, or a new one when there is none; or what is wrong with which field.</summary>
    /// <param name="parent">The Area <see cref="ParentId"/> names, with its own parents; null when there is no such Area.</param>
    public Result<Area> ApplyTo(Area? area, Area? parent)
    {
        List<FieldError> errors = [];
        string name = errors.CheckRequiredName(Name, "An Area needs a name.");
        if (ParentId is not null && parent is null)
        {
            errors.Add(new FieldError("parentId", "That Area does not exist."));
        }
        else if (area is not null && !area.CanSitInside(parent))
        {
            errors.Add(new FieldError("parentId", $"{area.Name} cannot sit inside itself or an Area inside it."));
        }
        // Result only changes the Area when nothing was wrong
        return new Result<Area>(errors, () =>
        {
            if (area is null)
            {
                return new Area(name, parent);
            }
            area.Name = name;
            area.Parent = parent;
            return area;
        });
    }
}
