using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Quests.Queries.GetQuest;
using ProjectX.Domain.Entities;
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
            .Select(x => new
            {
                x.Id,
                x.PreviousQuestId,
                x.Type,
                x.GameObjectName,
                x.Requirement,
                x.Reward   
            })
            .ToListAsync(cancellationToken);

        var language = _currentUserService.Language;

        return new GetQuestsDto
        {
            Quests = quests.Select(x =>
            {
                var parameters = x.Id.GetParameters();

                // TODO: translate service
                return new QuestDto
                {
                    Id = x.Id,
                    PreviousQuestId = x.PreviousQuestId,
                    Type = x.Type,
                    Title = _translateService.GetByKey($"{x.Id}Title", language),
                    Description = _translateService.GetByKey($"{x.Id}Description", language),
                    CompleteDescription = _translateService.GetByKey($"{x.Id}CompleteDescription", language),
                    StatusText = _translateService.GetByKey($"{x.Id}StatusText", language),
                    GameObjectName = x.GameObjectName,
                    Requirement = x.Requirement,
                    Reward = x.Reward
                };
            }).ToList()
        };
    }
}
