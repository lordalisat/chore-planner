using Microsoft.AspNetCore.Http.HttpResults;

namespace Chores.Api.Dtos;

/// <summary>
/// What validating a request gives: the value it asks for, or what was wrong with which field of it, never both.
/// Errors are keyed by the field's JSON name, so the form shows each at its field.
/// </summary>
public sealed class Result<T> where T : class
{
    private Result(T? value, IReadOnlyDictionary<string, string[]> errors)
    {
        Value = value;
        Errors = errors;
    }

    /// <summary>Null when there are errors.</summary>
    public T? Value { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static Result<T> Valid(T value) => new(value, new Dictionary<string, string[]>());

    public static Result<T> Invalid(IReadOnlyDictionary<string, string[]> errors) => new(null, errors);

    public ValidationProblem ToProblem() => TypedResults.ValidationProblem(Errors);
}
