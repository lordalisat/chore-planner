using Microsoft.AspNetCore.Http.HttpResults;

namespace Chores.Api.Dtos;

/// <summary>What validating a request gives: the value it asks for, or what was wrong with which field, never both.</summary>
public sealed class Result<T> where T : class
{
    /// <param name="build">Makes the value from the request; only called when there are no errors.</param>
    public Result(IReadOnlyList<FieldError> errors, Func<T> build)
    {
        Errors = errors;
        Value = errors.Count == 0 ? build() : null;
    }

    /// <summary>Null when there are errors.</summary>
    public T? Value { get; }

    public IReadOnlyList<FieldError> Errors { get; }

    public ValidationProblem ToProblem() => TypedResults.ValidationProblem(
        Errors.GroupBy(error => error.Field).ToDictionary(field => field.Key, field => field.Select(error => error.Message).ToArray()));
}
