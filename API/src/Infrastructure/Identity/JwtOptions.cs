using System.Text;

namespace ProjectX.Infrastructure.Identity;

public sealed record JwtOptions
{
    public const int MinimumSecurityKeySizeInBytes = 32;

    public JwtOptions(string securityKey, string validIssuer, string validAudience)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(securityKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(validIssuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(validAudience);

        if (Encoding.UTF8.GetByteCount(securityKey) < MinimumSecurityKeySizeInBytes)
        {
            throw new ArgumentException(
                $"The JWT security key must contain at least {MinimumSecurityKeySizeInBytes} UTF-8 bytes.",
                nameof(securityKey));
        }

        SecurityKey = securityKey;
        ValidIssuer = validIssuer;
        ValidAudience = validAudience;
    }

    public string SecurityKey { get; }

    public string ValidIssuer { get; }

    public string ValidAudience { get; }
}
