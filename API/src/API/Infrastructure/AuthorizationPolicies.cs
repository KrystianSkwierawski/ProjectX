namespace ProjectX.API.Infrastructure;

public static class AuthorizationPolicies
{
    public const string Client = nameof(Client);
    public const string Server = nameof(Server);
    public const string ServerOrClient = nameof(ServerOrClient);
    public const string ServerPlayerSession = nameof(ServerPlayerSession);
}
