using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

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
        var userId = _currentUserService.GetId();

        var character = await _context.Characters
            //.Where(x => x.Id = request.CharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .OrderByDescending(x => x.ModDate)
            .Select(x => new
            {
                Id = x.Id,
                Health = x.Health,
                Name = x.Name,
                CharacterExperiences = x.CharacterExperiences
                    .Select(x => new
                    {
                        x.Type,
                        x.Amount
                    }).ToList()
            })
            .SingleAsync(cancellationToken);

        return new CharacterDto
        {
            Name = character.Name,
            Health = character.Health,
            Levels = Enum.GetValues<ExperienceTypeEnum>()
                .Where(x => x != ExperienceTypeEnum.None)
                .ToDictionary(type => type, type => AddCharacterExperienceCommandHandler.GetLevel(character.CharacterExperiences
                    .Where(x => x.Type == type)
                    .Sum(x => x.Amount)
                )
            )
        };
    }
}