namespace Tomatime.Api.Services;

public static class SessionTiming
{
    public static int TotalMinutes(int focusMinutes) =>
        focusMinutes + (focusMinutes / 25 - 1) * 5;

    public static int EarnedPomos(int focusMinutes) => focusMinutes / 25 * 2;
}
