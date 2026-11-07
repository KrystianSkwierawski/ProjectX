using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.AbandonCharacterQuest;

public record AbandonCharacterQuestCommand(int CharacterQuestId) : IRequest<AbandonCharacterQuestDto>;

public class AbandonCharacterQuestCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<AbandonCharacterQuestCommandHandler> logger)
    : IRequestHandler<AbandonCharacterQuestCommand, AbandonCharacterQuestDto>
{
    public async Task<AbandonCharacterQuestDto> Handle(AbandonCharacterQuestCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetId();
        var characterId = currentUserService.GetRequiredCharacterId();

        await context.Characters
            .Where(character => character.Id == characterId)
            .Where(character => character.ApplicationUserId == userId && character.Status == StatusEnum.Active)
            .SingleOrNotFoundAsync("character", cancellationToken);

        var quest = await context.CharacterQuests
            .Where(quest => quest.Id == request.CharacterQuestId && quest.CharacterId == characterId)
            .SingleOrDefaultAsync(cancellationToken);

        if (quest == null)
        {
            logger.LogInformation("Quest abandonment already settled. CharacterId: {CharacterId}, CharacterQuestId: {CharacterQuestId}.", characterId, request.CharacterQuestId);

            return new AbandonCharacterQuestDto { CharacterQuestId = request.CharacterQuestId, Abandoned = true };
        }

        if (quest.Status is not (CharacterQuestStatusEnum.Accepted or CharacterQuestStatusEnum.Finished))
        {
            logger.LogInformation("Quest abandonment rejected. CharacterId: {CharacterId}, CharacterQuestId: {CharacterQuestId}, Status: {Status}.", characterId, quest.Id, quest.Status);

            return new AbandonCharacterQuestDto { CharacterQuestId = quest.Id };
        }

        context.CharacterQuests.Remove(quest);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogInformation("Quest abandonment rejected after concurrent change. CharacterId: {CharacterId}, CharacterQuestId: {CharacterQuestId}.", characterId, request.CharacterQuestId);

            return new AbandonCharacterQuestDto { CharacterQuestId = request.CharacterQuestId };
        }

        logger.LogInformation("Quest abandoned. CharacterId: {CharacterId}, CharacterQuestId: {CharacterQuestId}, QuestId: {QuestId}.", characterId, quest.Id, quest.QuestId);

        return new AbandonCharacterQuestDto { CharacterQuestId = quest.Id, Abandoned = true };
    }
}
