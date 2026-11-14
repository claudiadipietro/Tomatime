namespace Tomatime.Api.Services;

public static class SessionDuration
{
    public static bool IsValid(int duration) => duration is >= 25 and <= 300 && duration % 25 == 0;
}
