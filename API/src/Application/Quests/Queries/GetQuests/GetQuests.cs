using MediatR;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Quests.Queries.GetQuest;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Quests.Queries.GetQuests;

public record GetQuestsQuery : IRequest<GetQuestsDto>;

public class GetQuestsQueryHandler : IRequestHandler<GetQuestsQuery, GetQuestsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITranslateService _translateService;

    public GetQuestsQueryHandler(IApplicationDbContext context, ITranslateService translateService, ICurrentUserService currentUserService)
    {
        _context = context;
        _translateService = translateService;
        _currentUserService = currentUserService;
    }

    public async Task<GetQuestsDto> Handle(GetQuestsQuery request, CancellationToken cancellationToken)
    {
        var quests = await _context.Quests
            .Where(x => x.Status == StatusEnum.Active)
            .ToListAsync(cancellationToken);

        return new GetQuestsDto
        {
            Quests = quests
                .Select(x => QuestDto.From(x, _translateService, _currentUserService.Language))
                .ToArray()
        };
    }
}
