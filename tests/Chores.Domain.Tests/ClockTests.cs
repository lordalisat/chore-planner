using Xunit;

namespace Chores.Domain.Tests;

public class ClockTests
{
    private sealed class UtcClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void Today_is_the_date_in_Amsterdam_not_in_UTC()
    {
        var newYearsEveLateInUtc = new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2027, 1, 1), new UtcClock(newYearsEveLateInUtc).Today());
    }

    [Fact]
    public void Today_follows_summer_time_in_Amsterdam()
    {
        var lateEveningInUtc = new DateTimeOffset(2027, 7, 1, 22, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2027, 7, 2), new UtcClock(lateEveningInUtc).Today());
    }
}
