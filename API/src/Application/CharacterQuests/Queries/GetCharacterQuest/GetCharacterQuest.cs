using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.CharacterQuests.Queries.GetCharacterQuest;
public record GetCharacterQuestQuery(int CharacterId) : IRequest<CharacterQuestDto>;

public class GetCharacterQuestHandler : IRequestHandler<GetCharacterQuestQuery, CharacterQuestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCharacterQuestHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CharacterQuestDto> Handle(GetCharacterQuestQuery request, CancellationToken cancellationToken)
    {
        return await _context.CharacterQuests
            .Where(x => x.CharacterId == request.CharacterId)
            .Where(x => x.Character.ApplicationUserId == _currentUserService.Id)
            .Select(x => new CharacterQuestDto
            {
                Id = x.Id,
                Status = x.Status,
                Progress = x.Progress,
            })
            .SingleAsync(cancellationToken);
    }
}
