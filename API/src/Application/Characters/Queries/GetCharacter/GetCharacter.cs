using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Characters.Queries.GetCharacter;

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
                x.Id,
                x.Health,
                x.MaxHealth,
                x.Strength,
                x.Agility,
                x.Stamina,
                x.Intellect,
                x.Spirit,
                x.Armor,
                x.Helmet,
                x.Chest,
                x.Boots,
                x.Weapon,
                x.Name,
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
            MaxHealth = character.MaxHealth,
            Strength = character.Strength,
            Agility = character.Agility,
            Stamina = character.Stamina,
            Intellect = character.Intellect,
            Spirit = character.Spirit,
            Armor = character.Armor,
            Helmet = character.Helmet,
            Chest = character.Chest,
            Boots = character.Boots,
            Weapon = character.Weapon,
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
