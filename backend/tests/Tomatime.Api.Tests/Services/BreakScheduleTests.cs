using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class BreakScheduleTests
{
    [Theory]
    [InlineData(1499, 0, false)]
    [InlineData(1500, 0, true)]
    [InlineData(2099, 0, true)]
    [InlineData(2100, 1, false)]
    [InlineData(3600, 1, true)]
    [InlineData(4500, 2, false)]
    public void CalculatesPhasesWithExtraBreaks(int elapsed, int block, bool inBreak)
    {
        Assert.Equal((block, inBreak), BreakSchedule.At(75, [1, 2], TimeSpan.FromSeconds(elapsed)));
    }

    [Fact]
    public void IncludesAllExtraBreaksInTotalDuration()
    {
        Assert.Equal(100, BreakSchedule.TotalMinutes(75, [1, 2]));
    }
}
