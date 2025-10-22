using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterExperiences.Commands.AddCharacterExperience;
public record AddCharacterExperienceCommand : IRequest<AddCharacterExperienceDto>
{
    public int CharacterId { get; set; }

    public int CharacterQuestId { get; set; }

    public ExperienceTypeEnum Type { get; init; }

    public string ClientToken { get; init; }
}

public class AddCharacterExperienceCommandHandler : IRequestHandler<AddCharacterExperienceCommand, AddCharacterExperienceDto>
{
    private static readonly SortedDictionary<int, byte> _experienceToLevel = new SortedDictionary<int, byte>
    {
        { 0, 1 },
        { 100, 2 },
        { 400, 3 },
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

        int? amount = null;

        var userId = GetUserId(request.ClientToken);

        var now = DateTime.Now;

        if (request.Type == ExperienceTypeEnum.Questing)
        {
            var characterQuest = await _context.CharacterQuests
                .Include(x => x.Quest)
                .Where(x => x.Id == request.CharacterQuestId)
                //.Where(x => x.CharacterId == request.CharacterId)
                .Where(x => x.Status == CharacterQuestStatusEnum.Finished)
                .Where(x => x.Character.ApplicationUserId == userId)
                .SingleAsync(cancellationToken);

            characterQuest.EndDate = now;
            characterQuest.ModDate = now;
            characterQuest.Status = CharacterQuestStatusEnum.Completed;

            amount = characterQuest.Quest.Reward;

            Log.Debug("Completed character quest. CharacterQuestId: {0}, UserId: {1}", characterQuest.Id, userId);
        }

        var character = await _context.Characters
            .Include(x => x.CharacterExperiences)
            //.Where(x => x.Id == request.CharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .FirstAsync(cancellationToken);

        Log.Debug("Found character. CharacterId {0}, UserId: {1}", character.Id, userId);

        character.CharacterExperiences.Add(new CharacterExperience
        {
            Amount = amount ?? 50,
            Type = request.Type,
            ModDate = now
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
