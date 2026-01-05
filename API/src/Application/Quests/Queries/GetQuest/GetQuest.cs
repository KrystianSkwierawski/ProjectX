using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Quests.Queries.GetQuest;
public record GetQuestQuery(QuestEnum QuestId) : IRequest<QuestDto>;

public class GetQuestQueryHandler : IRequestHandler<GetQuestQuery, QuestDto>
{
    private readonly IApplicationDbContext _context;

    public GetQuestQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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

        var parameters = quest.Id.GetQuestParametersAttribute();

        return new QuestDto
        {
            Id = quest.Id,
            PreviousQuestId = quest.PreviousQuestId,
            Type = quest.Type,
            Title = parameters.Title,
            Description = parameters.Description,
            CompleteDescription = parameters.CompleteDescription,
            StatusText = parameters.StatusText,
            GameObjectName = quest.GameObjectName,
            Requirement = quest.Requirement,
            Reward = quest.Reward
        };
    }
}
