using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
public record AddCharacterExperienceCommand : IRequest<AddCharacterExperienceDto>
{
    public int Amount { get; init; }

    public ExperienceTypeEnum Type { get; init; }

    public string ClientToken { get; init; }
}

public class AddCharacterExperienceCommandHandler : IRequestHandler<AddCharacterExperienceCommand, AddCharacterExperienceDto>
{
    private static readonly SortedDictionary<int, byte> _experienceToLevel = new SortedDictionary<int, byte>
    {
        { 0, 1 },
        { 2000, 2 },
        { 3000, 3 },
        { 4000, 4 },
        { 5000, 5 },
        { 6000, 6 },
        { 7000, 7 },
        { 8000, 8 },
        { 9000, 9 },
        { 10000, 10 }
    };

    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<AddCharacterExperienceCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly TokenValidationParameters _validationParameters;

    public AddCharacterExperienceCommandHandler(IApplicationDbContext context, TokenValidationParameters validationParameters)
    {
        _context = context;
        _validationParameters = validationParameters;
    }

    public async Task<AddCharacterExperienceDto> Handle(AddCharacterExperienceCommand request, CancellationToken cancellationToken)
    {
        var result = new AddCharacterExperienceDto();

        var userId = GetUserId(request.ClientToken);

        var character = await _context.Characters
            .Include(x => x.CharacterExperiences)
            .Where(x => x.ApplicationUserId == userId)
            .FirstAsync(cancellationToken);

        Log.Debug("Found character. CharacterId {0}, UserId: {1}", character.Id, userId);

        character.CharacterExperiences.Add(new Domain.Entities.CharacterExperience
        {
            Amount = request.Amount,
            Type = request.Type,
            ModDate = DateTime.Now
        });

        result.Experience = character.CharacterExperiences
            .Select(x => x.Amount)
            .Sum();

        var newLevel = _experienceToLevel
            .Where(x => x.Key <= result.Experience)
            .Max(x => x.Value);

        if (character.Level < newLevel)
        {
            var diff = (byte)(newLevel - character.Level);
            character.Level = newLevel;
            character.SkillPoints += diff;

            result.LeveledUp = true;

            Log.Debug("LeveledUp. CharacterId: {0}, LevelDiff: {1}", character.Id, diff);
        }

        await _context.SaveChangesAsync(cancellationToken);

        result.Level = character.Level;
        result.SkillPoints = character.SkillPoints;

        return result;
    }

    private string GetUserId(string clientToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(clientToken, _validationParameters, out var validatedToken);

        var userId = principal.Claims
            .Where(x => x.Type == ClaimTypes.NameIdentifier)
            .Select(x => x.Value)
            .First();

        return userId;
    }
}
