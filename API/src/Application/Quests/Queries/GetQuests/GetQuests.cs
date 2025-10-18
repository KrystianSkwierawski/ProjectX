using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Application.Quests.Queries.GetQuest;

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
        var result = await _context.Quests
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
            .ToListAsync(cancellationToken);

        return new GetQuestsDto
        {
            Quests = result
        };
    }
}
