using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CheckCharacterQuestProgres;
public record CheckCharacterQuestProgressCommand(QuestEnum QuestId, int Progress, int CharacterId) : IRequest<CheckCharacterQuestProgressDto>;

public class CheckCharacterQuestProgressCommandHandler : IRequestHandler<CheckCharacterQuestProgressCommand, CheckCharacterQuestProgressDto>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<CheckCharacterQuestProgressCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CheckCharacterQuestProgressCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CheckCharacterQuestProgressDto> Handle(CheckCharacterQuestProgressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var characterQuest = _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.QuestId == request.QuestId)
            .Where(x => x.Character.ApplicationUserId == userId)
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted)
            .FirstOrDefault();

        if (characterQuest == null)
        {
            Log.Debug("Not found any active quests");
            return new CheckCharacterQuestProgressDto();
        }

        Log.Debug("Found character quest. CharacterQuestId: {0}, QuestId: {1}", characterQuest.Id, characterQuest.QuestId);

        characterQuest.Progress += request.Progress;
        characterQuest.ModDate = DateTime.Now;

        if (characterQuest.Progress >= characterQuest.Quest.Requirement)
        {
            Log.Debug("Completed character quest. CharacterQuestId: {0}, QuestId: {1}", characterQuest.Id, characterQuest.QuestId);

            characterQuest.Status = CharacterQuestStatusEnum.Finished;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CheckCharacterQuestProgressDto
        {
            QuestId = characterQuest.QuestId,
            CharacterQuestId = characterQuest.Id,
            Status = characterQuest.Status
        };
    }
}