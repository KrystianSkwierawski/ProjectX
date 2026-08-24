using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Characters.Queries.GetCharacters;

public record GetCharactersQuery : IRequest<GetCharactersDto>;

public class GetCharactersQueryHandler : IRequestHandler<GetCharactersQuery, GetCharactersDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCharactersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetCharactersDto> Handle(GetCharactersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var characters = await _context.Characters
            .Where(x => x.ApplicationUserId == userId)
            .Where(x => x.Status == StatusEnum.Active)
            .OrderBy(x => x.Id)
            .Select(x => new CharacterSummaryDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync(cancellationToken);

        return new GetCharactersDto
        {
            Characters = characters
        };
    }
}
