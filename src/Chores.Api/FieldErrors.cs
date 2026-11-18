using Microsoft.AspNetCore.Http.HttpResults;

namespace Chores.Api;

/// <summary>What was wrong with which field of a request, keyed by its JSON name, for the form to show at that field.</summary>
public sealed class FieldErrors
{
    private readonly Dictionary<string, string[]> _errors = [];

    public bool Any => _errors.Count > 0;

    public void Add(string field, string message) => _errors[field] = [message];

    /// <summary>Builds a Domain value, or records the message at the field when the Domain rejects it.</summary>
    public T? Try<T>(string field, string message, Func<T> build) where T : class
    {
        try
        {
            return build();
        }
        catch (ArgumentException)
        {
            Add(field, message);
            return null;
        }
    }

    public ValidationProblem ToProblem() => TypedResults.ValidationProblem(_errors);
}
