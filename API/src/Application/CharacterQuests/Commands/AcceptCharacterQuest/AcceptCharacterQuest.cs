using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AcceptCharacterQuest;
public record AcceptCharacterQuestCommand(int QuestId) : IRequest<int>;

public class AcceptCharacterQuestCommandHandler : IRequestHandler<AcceptCharacterQuestCommand, int>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<AcceptCharacterQuestCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AcceptCharacterQuestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(AcceptCharacterQuestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var characterId = await _context.Characters
            .Where(x => x.ApplicationUserId == userId)
            .Select(x => x.Id)
            .FirstAsync();

        Log.Debug("AcceptCharacterQuest -> Found character for id: {0}", characterId);

        var entity = new CharacterQuest
        {
            QuestId = request.QuestId,
            CharacterId = characterId,
            Status = CharacterQuestStatusEnum.Accepted,
            StartDate = DateTime.Now,
        };

        _context.CharacterQuests.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("AcceptCharacterQuest -> Accepted quest for id: {0}", request.QuestId);

        return entity.Id;
    }
}