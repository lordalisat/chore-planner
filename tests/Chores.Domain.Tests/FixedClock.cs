namespace Chores.Domain.Tests;

/// <summary>A clock that is always midday UTC+1 on the given date, which is that date in Amsterdam all year.</summary>
public sealed class FixedClock(DateOnly today) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        new DateTimeOffset(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(1)).ToUniversalTime();
}
