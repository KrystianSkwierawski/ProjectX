using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CharacterQuests.Commands.CompleteCharacterQuest;

public record CompleteCharacterQuestCommand(int CharacterQuestId) : IRequest;

public class CompleteCharacterQuestCommandHandler : IRequestHandler<CompleteCharacterQuestCommand>
{
    private static readonly Serilog.ILogger Log = Serilog.Log.ForContext<CompleteCharacterQuestCommandHandler>();

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CompleteCharacterQuestCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(CompleteCharacterQuestCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetId();

        var characterQuest = await _context.CharacterQuests
            .Where(x => x.Id == request.CharacterQuestId)
            .Where(x => x.Status == CharacterQuestStatusEnum.Finished)
            .Where(x => x.Character.ApplicationUserId == userId)
            .SingleAsync(cancellationToken);

        Log.Debug("Found character quest for id: {0}", characterQuest.Id);

        characterQuest.EndDate = DateTime.Now;
        characterQuest.ModDate = characterQuest.EndDate;
        characterQuest.Status = CharacterQuestStatusEnum.Completed;

        await _context.SaveChangesAsync(cancellationToken);

        Log.Debug("Completed character quest for id: {0}", characterQuest.Id);
    }
}
