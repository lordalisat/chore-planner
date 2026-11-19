namespace Chores.Api.Dtos;

/// <summary>What is wrong with one field of a request, keyed by its JSON name so the form shows it at that field.</summary>
public sealed record FieldError(string Field, string Message);

public static class FieldErrorList
{
    /// <summary>Builds a Domain value, or adds the message at the field when the Domain rejects it.</summary>
    public static T? Try<T>(this List<FieldError> errors, string field, string message, Func<T> build) where T : class
    {
        try
        {
            return build();
        }
        catch (ArgumentException)
        {
            errors.Add(new FieldError(field, message));
            return null;
        }
    }

    /// <summary>The trimmed Name, or adds at "name" why a Chore cannot have it.</summary>
    /// <param name="takenNames">The Names of the other Chores, which this one may not share.</param>
    public static string CheckName(this List<FieldError> errors, string? name, IEnumerable<string> takenNames)
    {
        // Missing when the JSON leaves it out, whatever its type says
        string trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            errors.Add(new FieldError("name", "A Chore needs a name."));
        }
        // A Name tells Chores apart on the Overview
        else if (takenNames.FirstOrDefault(taken => string.Equals(taken.Trim(), trimmed, StringComparison.CurrentCultureIgnoreCase)) is { } taken)
        {
            errors.Add(new FieldError("name", $"There is already a Chore called {taken}."));
        }
        return trimmed;
    }
}
