using MediatR;
using ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
using ProjectX.Application.Common.Extensions;
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
        var selectedCharacterId = _currentUserService.GetRequiredCharacterId();

        var character = await _context.Characters
            .Where(x => x.Id == selectedCharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .Where(x => x.Status == StatusEnum.Active)
            .SingleOrNotFoundAsync("character", cancellationToken);

        var entity = new CharacterQuest
        {
            QuestId = request.QuestId,
            CharacterId = character.Id,
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
