using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands;

public record AddCharacterQuestProgresCommand(int CharacterQuestId, int Progres, string ClientToken) : IRequest<AddCharacterQuestProgresDto>;

public class AddCharacterQuestProgresCommandHandler : IRequestHandler<AddCharacterQuestProgresCommand, AddCharacterQuestProgresDto>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<AddCharacterQuestProgresCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly TokenValidationParameters _validationParameters;

    public AddCharacterQuestProgresCommandHandler(IApplicationDbContext context, TokenValidationParameters validationParameters)
    {
        _context = context;
        _validationParameters = validationParameters;
    }

    public async Task<AddCharacterQuestProgresDto> Handle(AddCharacterQuestProgresCommand request, CancellationToken cancellationToken)
    {
        var userId = GetUserId(request.ClientToken);

        var characterQuest = await _context.CharacterQuests
            .Include(x => x.Quest)
            .Where(x => x.Id == request.CharacterQuestId)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found character quest. CharacterQuestId: {0}, QuestId: {1}", characterQuest.Id, characterQuest.QuestId);

        characterQuest.Progress += request.Progres;
        characterQuest.ModDate = DateTime.Now;

        if (characterQuest.Progress >= characterQuest.Quest.Requirements)
        {
            Log.Debug("Completed character quest. CharacterQuestId: {0}, QuestId: {1}", characterQuest.Id, characterQuest.QuestId);

            characterQuest.Status = CharacterQuestStatusEnum.Completed;
            characterQuest.EndDate = characterQuest.ModDate;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new AddCharacterQuestProgresDto
        {
            Status = characterQuest.Status,
            Reward = characterQuest.Status == CharacterQuestStatusEnum.Completed ? characterQuest.Quest.Reward : 0
        };
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
