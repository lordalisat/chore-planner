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
}
