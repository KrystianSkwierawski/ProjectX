using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Characters;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;

public record AddCharacterExperienceCommand : IRequest<AddCharacterExperienceDto>
{
    public int CharacterId { get; set; }
    public int Amount { get; set; }
    public ExperienceTypeEnum Type { get; init; }
}

public class AddCharacterExperienceCommandHandler : IRequestHandler<AddCharacterExperienceCommand, AddCharacterExperienceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddCharacterExperienceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AddCharacterExperienceDto> Handle(AddCharacterExperienceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();
        var character = await _context.Characters
            .Include(x => x.CharacterExperiences.Where(experience => experience.Type == request.Type))
            .Where(x => x.Id == request.CharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character", cancellationToken);

        character.CharacterExperiences.Add(new CharacterExperience { Amount = request.Amount, Type = request.Type });
        var experience = character.CharacterExperiences.Sum(entry => entry.Amount);

        await _context.SaveChangesAsync(cancellationToken);

        return new AddCharacterExperienceDto
        {
            Experience = experience,
            Level = ExperienceProgression.GetLevel(experience)
        };
    }
}
