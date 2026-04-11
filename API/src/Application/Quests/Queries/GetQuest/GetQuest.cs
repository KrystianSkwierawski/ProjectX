using MediatR;
using Microsoft.EntityFrameworkCore;
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
            .Select(x => new
            {
                x.Id,
                x.PreviousQuestId,
                x.Type,
                x.GameObjectName,
                x.Requirement,
                x.Reward
            })
            .SingleAsync(cancellationToken);

        var parameters = quest.Id.GetParameters();
        var language = _currentUserService.Language;

        // TODO: translate service
        return new QuestDto
        {
            Id = quest.Id,
            PreviousQuestId = quest.PreviousQuestId,
            Type = quest.Type,
            Title = _translateService.GetByKey($"{quest.Id}Title", language),
            Description = _translateService.GetByKey($"{quest.Id}Description", language),
            CompleteDescription = _translateService.GetByKey($"{quest.Id}CompleteDescription", language),
            StatusText = _translateService.GetByKey($"{quest.Id}StatusText", language),
            GameObjectName = quest.GameObjectName,
            Requirement = quest.Requirement,
            Reward = quest.Reward
        };
    }
}
