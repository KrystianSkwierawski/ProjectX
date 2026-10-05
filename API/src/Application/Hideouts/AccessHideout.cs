using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.Hideouts;

public record AccessHideoutCommand(HideoutBuildingEnum Building = HideoutBuildingEnum.None) : IRequest<HideoutDto>
{
    public override string ToString() => $"Hideout {{ Building = {Building} }}";
}

public class AccessHideoutCommandHandler(IApplicationDbContext context, ICurrentUserService user,
    TimeProvider time, ILogger<AccessHideoutCommandHandler> logger) : IRequestHandler<AccessHideoutCommand, HideoutDto>
{
    public async Task<HideoutDto> Handle(AccessHideoutCommand request, CancellationToken cancellationToken)
    {
        var characterId = user.GetRequiredCharacterId();

        var userId = user.GetId();

        var inventory = await context.CharacterInventories
            .Where(x => x.Id == characterId && x.Character.ApplicationUserId == userId)
            .SingleOrNotFoundAsync("character inventory", cancellationToken);

        var definitions = await context.HideoutBuildingTypes
            .AsNoTracking()
            .Where(x => x.Status == StatusEnum.Active)
            .OrderBy(x => x.Id)
            .ToArrayAsync(cancellationToken);

        var buildings = await context.CharacterHideouts
            .AsNoTracking()
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        var outcome = "Applied";

        if (request.Building != HideoutBuildingEnum.None)
        {
            var definition = definitions.Where(x => x.Id == request.Building).SingleOrDefault();
            var remaining = inventory.Inventory.Clone();

            if (buildings.Any(x => x.HideoutBuildingTypeId == request.Building))
            {
                outcome = "AlreadyBuilt";
            }
            else if (definition == null || definition.BuildTime <= 0)
            {
                outcome = "Unavailable";
            }
            else if (!definition.Requirement.Items.All(x => remaining.Remove(x.Type, x.Count)))
            {
                outcome = "MissingMaterials";
            }
            else
            {
                var now = time.GetUtcNow();

                var building = new CharacterHideout
                {
                    CharacterId = characterId,
                    HideoutBuildingTypeId = definition.Id,
                    BuildStartedAt = now,
                    BuildEndsAt = now.AddSeconds(definition.BuildTime)
                };

                var quests = await context.CharacterQuests.Include(x => x.Quest)
                    .Where(x => x.CharacterId == characterId && x.Quest.Type == QuestTypeEnum.Collect)
                    .Where(x => x.Status == CharacterQuestStatusEnum.Accepted || x.Status == CharacterQuestStatusEnum.Finished)
                    .ToArrayAsync(cancellationToken);

                foreach (var quest in quests)
                {
                    quest.SetProgress(remaining.GetCount(Enum.Parse<InventoryItemEnum>(quest.Quest.GameObjectName)), quest.Quest.Requirement);
                }

                inventory.Inventory = remaining;
                context.CharacterHideouts.Add(building);

                // One EF save atomically commits inventory, Collect progress and the unique building.
                // On concurrency/unique-key failure the request fails; the caller reloads before retrying.
                await context.SaveChangesAsync(cancellationToken);

                buildings.Add(building);
            }

            logger.LogInformation("Hideout construction {Outcome}. CharacterId: {CharacterId}, Building: {Building}",
                outcome, characterId, request.Building);
        }

        return new HideoutDto
        {
            Outcome = outcome,
            CurrentTime = time.GetUtcNow(),
            Buildings = definitions.Select(definition =>
            {
                var state = buildings.Where(x => x.HideoutBuildingTypeId == definition.Id).SingleOrDefault();

                return new HideoutBuildingDto
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    Requirement = definition.Requirement,
                    BuildTime = definition.BuildTime,
                    BuildStartedAt = state?.BuildStartedAt,
                    BuildEndsAt = state?.BuildEndsAt
                };
            }).ToArray(),
            CharacterInventory = new CharacterInventoryDto
            {
                CharacterId = characterId,
                Count = (short)Math.Max(inventory.Count, inventory.Inventory.Items.Count),
                Inventory = new InventoryDto
                {
                    Items = inventory.Inventory.Items.Select(x => new InventoryItemDto { Type = x.Type, Count = x.Count }).ToArray()
                }
            }
        };
    }
}
