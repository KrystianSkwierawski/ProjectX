using MediatR;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Quests.Queries.GetQuest;

public record GetQuestQuery(QuestEnum QuestId) : IRequest<QuestDto>;

public class GetQuestQueryHandler : IRequestHandler<GetQuestQuery, QuestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITranslateService _translateService;

    public GetQuestQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ITranslateService translateService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _translateService = translateService;
    }

    public async Task<QuestDto> Handle(GetQuestQuery request, CancellationToken cancellationToken)
    {
        var quest = await _context.Quests
            .Where(x => x.Id == request.QuestId)
            .SingleOrNotFoundAsync("quest", cancellationToken);

        return QuestDto.From(quest, _translateService, _currentUserService.Language);
    }
}
