namespace ProjectX.Application.Common.Exceptions;

public sealed class InvalidGameSessionCredentialException : Exception
{
    public InvalidGameSessionCredentialException()
        : base("The game-session credential is invalid, expired, or has already been used.")
    {
    }
}
