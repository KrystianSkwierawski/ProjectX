namespace ProjectX.Infrastructure.Identity;

public sealed record JwtOptions(string SecurityKey, string ValidIssuer, string ValidAudience);
