namespace Chores.Api.Dtos;

/// <summary>Collects what is wrong with which field while a request validates itself, ending in a <see cref="Result{T}"/>.</summary>
public sealed class FieldErrors
{
    private readonly Dictionary<string, string[]> _errors = [];

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

    /// <summary>The errors so far, or, when there are none, what <paramref name="build"/> makes.</summary>
    public Result<T> ToResult<T>(Func<T> build) where T : class =>
        _errors.Count > 0 ? Result<T>.Invalid(_errors) : Result<T>.Valid(build());
}
