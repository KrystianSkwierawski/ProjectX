using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
using ProjectX.Application.Common.Exceptions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;

public record AcceptCharacterQuestCommand(QuestEnum QuestId) : IRequest<CharacterQuestDto>;

public class AcceptCharacterQuestCommandHandler : IRequestHandler<AcceptCharacterQuestCommand, CharacterQuestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;

    public AcceptCharacterQuestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, TimeProvider timeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
    }

    public async Task<CharacterQuestDto> Handle(AcceptCharacterQuestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();
        var characterId = await _context.Characters
            .Where(character => character.ApplicationUserId == userId)
            .Select(character => (int?)character.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("character");

        var entity = new CharacterQuest
        {
            QuestId = request.QuestId,
            CharacterId = characterId,
            Status = CharacterQuestStatusEnum.Accepted,
            StartDate = _timeProvider.GetUtcNow()
        };

        _context.CharacterQuests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new CharacterQuestDto
        {
            Id = entity.Id,
            QuestId = entity.QuestId,
            Progress = entity.Progress,
            Status = entity.Status
        };
    }
}
