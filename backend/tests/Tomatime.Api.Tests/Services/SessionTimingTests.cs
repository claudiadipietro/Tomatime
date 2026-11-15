using Tomatime.Api.Services;

namespace Tomatime.Api.Tests;

public class SessionTimingTests
{
    [Theory]
    [InlineData(25, 25, 2)]
    [InlineData(50, 55, 4)]
    [InlineData(75, 85, 6)]
    [InlineData(300, 355, 24)]
    public void IncludesBreaksAndRewardsEachFocusBlock(int duration, int total, int pomos)
    {
        Assert.Equal(total, SessionTiming.TotalMinutes(duration));
        Assert.Equal(pomos, SessionTiming.EarnedPomos(duration));
    }
}
