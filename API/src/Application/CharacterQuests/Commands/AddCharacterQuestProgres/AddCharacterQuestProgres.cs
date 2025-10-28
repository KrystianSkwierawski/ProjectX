using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AddCharacterQuestProgres;

public record AddCharacterQuestProgresCommand(int CharacterQuestId, int Progres) : IRequest<AddCharacterQuestProgresDto>;

public class AddCharacterQuestProgresCommandHandler : IRequestHandler<AddCharacterQuestProgresCommand, AddCharacterQuestProgresDto>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<AddCharacterQuestProgresCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddCharacterQuestProgresCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AddCharacterQuestProgresDto> Handle(AddCharacterQuestProgresCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var characterQuest = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.Id == request.CharacterQuestId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found character quest. CharacterQuestId: {0}, QuestId: {1}", characterQuest.Id, characterQuest.QuestId);

        characterQuest.Progress += request.Progres;
        characterQuest.ModDate = DateTime.Now;

        if (characterQuest.Progress >= characterQuest.Quest.Requirement)
        {
            Log.Debug("Completed character quest. CharacterQuestId: {0}, QuestId: {1}", characterQuest.Id, characterQuest.QuestId);

            characterQuest.Status = CharacterQuestStatusEnum.Finished;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new AddCharacterQuestProgresDto
        {
            Status = characterQuest.Status,
            Reward = characterQuest.Status == CharacterQuestStatusEnum.Completed ? characterQuest.Quest.Reward : 0
        };
    }
}
