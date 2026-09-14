using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterSettings.Queries.GetCharacterSettings;

public record GetCharacterSettingsQuery(int CharacterId) : IRequest<CharacterSettingsDto>;

public class GetCharacterSettingsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetCharacterSettingsQuery, CharacterSettingsDto>
{
    public async Task<CharacterSettingsDto> Handle(GetCharacterSettingsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetId();

        await context.Characters.Where(x => x.Id == request.CharacterId)
            .Where(x => x.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character", cancellationToken);

        return await context.CharacterSettings.AsNoTracking()
            .Where(x => x.CharacterId == request.CharacterId)
            .Select(x => new CharacterSettingsDto
            {
                CharacterId = x.CharacterId,
                Language = x.Language,
                ActionBars = x.ActionBars
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? new CharacterSettingsDto
            {
                CharacterId = request.CharacterId,
                Language = currentUser.Language,
                ActionBars = new InventoryItemEnum[Domain.Entities.CharacterSettings.ActionBarSlotCount]
            };
    }
}
