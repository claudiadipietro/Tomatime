namespace Tomatime.Api.Services;

public static class BreakSchedule
{
    public static (int BreakIndex, bool InBreak) At(int duration, int[] extensions, TimeSpan elapsed)
    {
        double position = elapsed.TotalSeconds;
        for (var index = 0; index < duration / 25; index++)
        {
            if (position < 1500)
            {
                return (index, false);
            }

            position -= 1500;
            if (index == duration / 25 - 1)
            {
                return (index, false);
            }

            int length = 300 + (index < extensions.Length ? extensions[index] * 300 : 0);
            if (position < length)
            {
                return (index, true);
            }

            position -= length;
        }
        return (duration / 25 - 1, false);
    }

    public static int TotalMinutes(int duration, int[] extensions) =>
        SessionTiming.TotalMinutes(duration) + extensions.Sum() * 5;
}
