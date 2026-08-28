using ProjectX.Application.GameSessions.Models;

namespace ProjectX.Application.Common.Interfaces;

public interface IGameSessionService
{
    RegisteredGameSession Register(string serverUserId, bool usesRelay, string? relayJoinCode);

    RegisteredGameSession Heartbeat(string serverUserId, Guid gameSessionId);

    GameConnectionTicket CreateTicket(string clientUserId, int characterId);

    RedeemedGameSessionTicket Redeem(string serverUserId, Guid gameSessionId, string ticket);

    bool TryResolvePlayer(string serverUserId, string playerSessionId, out ResolvedPlayerSession playerSession);

    bool IsCharacterOnline(string serverUserId, int characterId);

    void RevokePlayer(string serverUserId, string playerSessionId);
}
