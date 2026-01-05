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

    public GetQuestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetQuestsDto> Handle(GetQuestsQuery request, CancellationToken cancellationToken)
    {
        var quest = await _context.Quests
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

        return new GetQuestsDto
        {
            Quests = quest.Select(x =>
            {
                // TODO: translation
                var parameters = x.Id.GetQuestParametersAttribute();

                return new QuestDto
                {
                    Id = x.Id,
                    PreviousQuestId = x.PreviousQuestId,
                    Type = x.Type,
                    Title = parameters.Title,
                    Description = parameters.Description,
                    CompleteDescription = parameters.CompleteDescription,
                    StatusText = parameters.StatusText,
                    GameObjectName = x.GameObjectName,
                    Requirement = x.Requirement,
                    Reward = x.Reward
                };
            }).ToList()
        };
    }
}
