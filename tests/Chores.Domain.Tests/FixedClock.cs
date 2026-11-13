namespace Chores.Domain.Tests;

/// <summary>A clock that is always noon on the given date in Amsterdam.</summary>
public sealed class FixedClock(DateOnly today) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        new DateTimeOffset(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(1)).ToUniversalTime();
}
