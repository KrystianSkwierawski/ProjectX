using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.GameSessions.Commands.CreateGameSessionTicket;

public record CreateGameSessionTicketCommand : IRequest<CreateGameSessionTicketDto>
{
    public int CharacterId { get; init; }

    public override string ToString()
    {
        return $"{nameof(CreateGameSessionTicketCommand)} {{ CharacterId = {CharacterId} }}";
    }
}

public class CreateGameSessionTicketCommandHandler : IRequestHandler<CreateGameSessionTicketCommand, CreateGameSessionTicketDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;
    private readonly IGameSessionService _gameSessionService;

    public CreateGameSessionTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IGameSessionService gameSessionService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _gameSessionService = gameSessionService;
    }

    public async Task<CreateGameSessionTicketDto> Handle(CreateGameSessionTicketCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetAuthenticatedUserId();

        var ownsCharacter = await _context.Characters
            .Where(x => x.Id == request.CharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .Where(x => x.Status == StatusEnum.Active)
            .AnyAsync(cancellationToken);

        if (!ownsCharacter)
        {
            throw new NotFoundException("character");
        }

        var ticket = _gameSessionService.CreateTicket(userId, request.CharacterId);

        return new CreateGameSessionTicketDto
        {
            GameSessionId = ticket.GameSessionId,
            CharacterId = ticket.CharacterId,
            UsesRelay = ticket.UsesRelay,
            RelayJoinCode = ticket.RelayJoinCode,
            Ticket = ticket.Ticket,
            ExpiresAtUtc = ticket.ExpiresAtUtc
        };
    }
}
