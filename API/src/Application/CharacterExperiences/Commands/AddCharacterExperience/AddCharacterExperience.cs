using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
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
    private static readonly SortedDictionary<int, byte> ExperienceToLevel = new()
    {
        { 0, 1 }, { 100, 2 }, { 400, 3 }, { 4000, 4 }, { 5000, 5 },
        { 6000, 6 }, { 7000, 7 }, { 8000, 8 }, { 9000, 9 }, { 10000, 10 }
    };

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
            .Include(candidate => candidate.CharacterExperiences.Where(experience => experience.Type == request.Type))
            .Where(candidate => candidate.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character", cancellationToken);

        character.CharacterExperiences.Add(new CharacterExperience { Amount = request.Amount, Type = request.Type });
        var experience = character.CharacterExperiences.Sum(entry => entry.Amount);

        await _context.SaveChangesAsync(cancellationToken);

        return new AddCharacterExperienceDto
        {
            Experience = experience,
            Level = GetLevel(experience)
        };
    }

    public static byte GetLevel(int experience)
    {
        return ExperienceToLevel.Where(level => level.Key <= experience).Max(level => level.Value);
    }
}
