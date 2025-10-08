using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Characters.Queries;
public record GetCharacterQuery(int CharacterId) : IRequest<CharacterDto>;

public class GetCharacterQueryHandler : IRequestHandler<GetCharacterQuery, CharacterDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCharacterQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CharacterDto> Handle(GetCharacterQuery request, CancellationToken cancellationToken)
    {
        return await _context.Characters
            //.Where(x => x.Id = request.CharacterId)
            .Where(x => x.ApplicationUserId == _currentUserService.Id)
            .OrderByDescending(x => x.ModDate)
            .Select(x => new CharacterDto
            {
                Level = x.Level,
                SkillPoints = x.SkillPoints,
                Health = x.Health,
                Name = x.Name,
            })
            .SingleAsync(cancellationToken);
    }
}