using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterQuests.Queries.GetCharacterQuests;

public record GetCharacterQuestsQuery(int CharacterId) : IRequest<GetCharacterQuestsDto>;

public class GetCharacterQuestsHandler : IRequestHandler<GetCharacterQuestsQuery, GetCharacterQuestsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCharacterQuestsHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetCharacterQuestsDto> Handle(GetCharacterQuestsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var result = await _context.CharacterQuests
            .Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .Select(x => new CharacterQuestDto
            {
                Id = x.Id,
                QuestId = x.QuestId,
                Status = x.Status,
                Progress = x.Progress,
            })
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return new GetCharacterQuestsDto
        {
            CharacterQuests = result
        };
    }
}
