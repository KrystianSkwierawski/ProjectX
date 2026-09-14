using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterSettings.Commands.UpdateCharacterSettings;

public record UpdateCharacterSettingsCommand(int CharacterId, LanguageEnum Language, InventoryItemEnum[] ActionBars)
    : IRequest<CharacterSettingsDto>
{
    public override string ToString() => $"{nameof(UpdateCharacterSettingsCommand)} {{ CharacterId = {CharacterId}, Language = {Language} }}";
}

public class UpdateCharacterSettingsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<UpdateCharacterSettingsCommand, CharacterSettingsDto>
{
    public async Task<CharacterSettingsDto> Handle(UpdateCharacterSettingsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetId();

        await context.Characters.Where(x => x.Id == request.CharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character", cancellationToken);

        var settings = await context.CharacterSettings.SingleOrDefaultAsync(x => x.CharacterId == request.CharacterId, cancellationToken);

        if (settings == null)
        {
            settings = new Domain.Entities.CharacterSettings { CharacterId = request.CharacterId };
            context.CharacterSettings.Add(settings);
        }

        settings.Update(request.Language, request.ActionBars);

        await context.SaveChangesAsync(cancellationToken);

        return new CharacterSettingsDto
        {
            CharacterId = settings.CharacterId,
            Language = settings.Language,
            ActionBars = settings.ActionBars.ToArray()
        };
    }
}
