using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;
public record GetCharacterQuestsQuery(int CharacterId) : IRequest<GetCharacterQuestsDto>;

public class GetCharacterQuestsHandler : IRequestHandler<GetCharacterQuestsQuery, GetCharacterQuestsDto>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<GetCharacterQuestsHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCharacterQuestsHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetCharacterQuestsDto> Handle(GetCharacterQuestsQuery request, CancellationToken cancellationToken)
    {
        var characterQuests = await _context.CharacterQuests
            //.Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == _currentUserService.Id)
            .Select(x => new CharacterQuestDto
            {
                Id = x.Id,
                QuestId = x.QuestId,
                Status = x.Status,
                Progress = x.Progress,
            })
            .ToListAsync(cancellationToken);

        Log.Debug("Found character quests. CharacterId: {0}, Count: {1}", request.CharacterId, characterQuests.Count);

        return new GetCharacterQuestsDto
        {
            CharacterQuests = characterQuests
        };
    }
}
