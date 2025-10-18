using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Quests.Queries.GetQuest;
public record GetQuestQuery(int QuestId) : IRequest<QuestDto>;

public class GetQuestQueryHandler : IRequestHandler<GetQuestQuery, QuestDto>
{
    private readonly IApplicationDbContext _context;

    public GetQuestQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QuestDto> Handle(GetQuestQuery request, CancellationToken cancellationToken)
    {
        return await _context.Quests
            .Where(x => x.Id == request.QuestId)
            .Select(x => new QuestDto
            {
                Id = x.Id,
                Type = x.Type,
                Title = x.Title,
                Description = x.Description,
                StatusText = x.StatusText,
                GameObjectName = x.GameObjectName,
                Requirement = x.Requirement,
                Reward = x.Reward
            })
            .SingleAsync(cancellationToken);
    }
}
