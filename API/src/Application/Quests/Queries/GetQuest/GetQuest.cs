using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;

namespace ProjectX.Application.Quests.Queries.GetQuest;
public record GetQuestQuery(int QuestId) : IRequest<QuestoDto>;

public class GetQuestQueryHandler : IRequestHandler<GetQuestQuery, QuestoDto>
{
    private readonly IApplicationDbContext _context;

    public GetQuestQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QuestoDto> Handle(GetQuestQuery request, CancellationToken cancellationToken)
    {
        return await _context.Quests
            .Where(x => x.Id == request.QuestId)
            .Select(x => new QuestoDto
            {
                Type = x.Type,
                Title = x.Title,
                Description = x.Description,
                StatusText = x.StatusText,
                Reward = x.Reward
            })
            .SingleAsync(cancellationToken);
    }
}
