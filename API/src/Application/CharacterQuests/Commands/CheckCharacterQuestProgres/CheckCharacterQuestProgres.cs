using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CheckProgres;
public record CheckCharacterQuestProgresCommand(int CharacterId, string GameObjectName, int Progres) : IRequest<CheckCharacterQuestProgresDto>;

public class CheckCharacterQuestProgresCommandHandler : IRequestHandler<CheckCharacterQuestProgresCommand, CheckCharacterQuestProgresDto>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<CheckCharacterQuestProgresCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CheckCharacterQuestProgresCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CheckCharacterQuestProgresDto> Handle(CheckCharacterQuestProgresCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var characterQuest = _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.Character.ApplicationUserId == userId)
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Accepted)
            .Where(x => x.Quest.GameObjectName == request.GameObjectName)
            .FirstOrDefault();

        if (characterQuest == null)
        {
            Log.Debug("Not found any active quests");
            return new CheckCharacterQuestProgresDto();
        }

        Log.Debug("Found character quest. CharacterQuestId: {0}, QuestId: {1}", characterQuest.Id, characterQuest.QuestId);

        characterQuest.Progress += request.Progres;
        characterQuest.ModDate = DateTime.Now;

        if (characterQuest.Progress >= characterQuest.Quest.Requirement)
        {
            Log.Debug("Completed character quest. CharacterQuestId: {0}, QuestId: {1}", characterQuest.Id, characterQuest.QuestId);

            characterQuest.Status = CharacterQuestStatusEnum.Finished;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CheckCharacterQuestProgresDto
        {
            QuestId = characterQuest.QuestId,
            CharacterQuestId = characterQuest.Id,
            Status = characterQuest.Status
        };
    }
}