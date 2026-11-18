namespace Tomatime.Api.Tests;

public sealed class PushDatabaseFactAttribute : FactAttribute
{
    public PushDatabaseFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TOMATIME_PUSH_TEST_CONNECTION")))
        {
            Skip = "Set TOMATIME_PUSH_TEST_CONNECTION to an isolated local test database.";
        }
    }
}
