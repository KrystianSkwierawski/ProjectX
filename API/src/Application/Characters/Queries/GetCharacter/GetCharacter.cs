using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
using ProjectX.Application.Common.Extensions;
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
                x.Dexterity,
                x.Speed,
                x.Intellect,
                x.Armor,
                x.HelmetType,
                x.ChestType,
                x.BootsType,
                x.WeaponType,
                x.AmmoType,
                x.AmmoCount,
                x.Name,
                CharacterExperiences = x.CharacterExperiences
                    .Select(x => new
                    {
                        x.Type,
                        x.Amount
                    }).ToList()
            })
            .SingleOrNotFoundAsync("character", cancellationToken);

        return new CharacterDto
        {
            Name = character.Name,
            Health = character.Health,
            MaxHealth = character.MaxHealth,
            Strength = character.Strength,
            Dexterity = character.Dexterity,
            Speed = character.Speed,
            Intellect = character.Intellect,
            Armor = character.Armor,
            HelmetType = character.HelmetType,
            ChestType = character.ChestType,
            BootsType = character.BootsType,
            WeaponType = character.WeaponType,
            AmmoType = character.AmmoType,
            AmmoCount = character.AmmoCount,
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
