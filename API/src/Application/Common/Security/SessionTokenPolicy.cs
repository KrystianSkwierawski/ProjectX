namespace ProjectX.Application.Common.Security;

public static class SessionTokenPolicy
{
    public const string CurrentVersion = "1";
    public const string VersionClaim = "token_version";

    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
    public static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(5);
}
