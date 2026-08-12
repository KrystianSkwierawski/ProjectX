namespace ProjectX.Application.Common.Security;

public static class SessionTokenPolicy
{
    public const string CurrentVersion = "2";
    public const string SessionStartedAtClaim = "session_started_at";
    public const string VersionClaim = "token_version";

    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaximumSessionLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(5);
}
