using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Extensions;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Characters;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;
using ProjectX.Domain.Hideouts;
using ProjectX.Domain.Inventory;

namespace ProjectX.Application.Hideouts;

public record AccessHideoutCommand(HideoutBuildingEnum Building = HideoutBuildingEnum.None,
    HideoutOperationEnum Operation = HideoutOperationEnum.Build, int Slot = -1, int InventorySlot = -1,
    InventoryItemEnum Item = InventoryItemEnum.None, int Count = 0, long Revision = 0,
    double RespawnInterval = 5, int HarvestExperience = 0) : IRequest<HideoutDto>
{
    public override string ToString() => $"Hideout {{ Building = {Building}, Operation = {Operation}, Slot = {Slot} }}";
}

// TODO: Extract per-building state/operation handlers when adding the next hideout type.
// Data storage is generic; Farm dispatch and response projection are temporary.
public class AccessHideoutCommandHandler(IApplicationDbContext context, ICurrentUserService user,
    TimeProvider time, ILogger<AccessHideoutCommandHandler> logger, IHideoutStateSerializer stateSerializer) : IRequestHandler<AccessHideoutCommand, HideoutDto>
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
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        var farms = buildings
            .Where(x => x.HideoutBuildingTypeId == HideoutBuildingEnum.Farm)
            .ToDictionary(x => x.Id, x => (FarmState)DeserializeState(x)!);

        var dirty = new HashSet<CharacterHideout>();
        var now = time.GetUtcNow();

        foreach (var building in buildings
            .Where(x => x.HideoutBuildingTypeId == HideoutBuildingEnum.Farm && !x.IsUnderConstruction(now)))
        {
            if (farms[building.Id].Advance(now))
            {
                dirty.Add(building);
            }
        }

        var outcome = "Applied";
        var remaining = inventory.Inventory.Clone();
        var inventoryChanged = false;

        if (request.Building != HideoutBuildingEnum.None && request.Operation != HideoutOperationEnum.Read)
        {
            var definition = definitions
                .Where(x => x.Id == request.Building)
                .SingleOrDefault();

            var building = buildings
                .Where(x => x.HideoutBuildingTypeId == request.Building)
                .SingleOrDefault();

            if (definition == null)
            {
                outcome = "Unavailable";
            }
            else if (request.Operation == HideoutOperationEnum.Build)
            {
                if (building != null)
                {
                    outcome = "AlreadyBuilt";
                }
                else if (definition.BuildTime <= 0)
                {
                    outcome = "Unavailable";
                }
                else if (!definition.Requirement.Items.All(x => remaining.Remove(x.Type, x.Count)))
                {
                    outcome = "MissingMaterials";
                }
                else
                {
                    building = new CharacterHideout
                    {
                        CharacterId = characterId,
                        HideoutBuildingTypeId = definition.Id,
                        BuildStartedAt = now,
                        BuildEndsAt = now.AddSeconds(definition.BuildTime)
                    };

                    context.CharacterHideouts.Add(building);
                    buildings.Add(building);

                    if (definition.Id == HideoutBuildingEnum.Farm)
                    {
                        farms[building.Id] = new FarmState();
                        dirty.Add(building);
                    }

                    inventoryChanged = true;
                }
            }
            else if (building == null || building.IsUnderConstruction(now) || request.Building != HideoutBuildingEnum.Farm)
            {
                outcome = "Unavailable";
            }
            else
            {
                var farm = farms[building.Id];

                if (request.Revision != farm.Revision)
                {
                    outcome = "StaleState";
                }
                else
                {
                    outcome = farm.Apply(request.Operation, request.Slot, request.InventorySlot, request.Item, request.Count,
                        request.RespawnInterval, remaining, Math.Max(inventory.Count, remaining.Items.Count), now);

                    if (outcome == "Applied")
                    {
                        dirty.Add(building);
                        inventoryChanged = true;
                    }
                }
            }

            logger.LogInformation("Hideout {Operation} {Outcome}. CharacterId: {CharacterId}, Building: {Building}, Slot: {Slot}",
                request.Operation, outcome, characterId, request.Building, request.Slot);
        }

        if (inventoryChanged)
        {
            var quests = await context.CharacterQuests
                .Include(x => x.Quest)
                .Where(x => x.CharacterId == characterId && x.Quest.Type == QuestTypeEnum.Collect)
                .Where(x => x.Status == CharacterQuestStatusEnum.Accepted || x.Status == CharacterQuestStatusEnum.Finished)
                .ToArrayAsync(cancellationToken);

            foreach (var quest in quests)
            {
                quest.SetProgress(remaining.GetCount(Enum.Parse<InventoryItemEnum>(quest.Quest.GameObjectName)), quest.Quest.Requirement);
            }

            inventory.Inventory = remaining;
        }

        if (request.Operation == HideoutOperationEnum.Harvest && outcome == "Applied" && inventoryChanged && request.HarvestExperience > 0)
        {
            context.CharacterExperiences.Add(new CharacterExperience
            {
                CharacterId = characterId,
                Type = ExperienceTypeEnum.Herbalism,
                Amount = request.HarvestExperience
            });
        }

        foreach (var building in dirty)
        {
            var farm = farms[building.Id];

            farm.Revision++;
            building.Data = stateSerializer.Serialize(farm);
        }

        if (inventoryChanged || dirty.Count > 0)
        {
            // Inventory, Collect progress, timers and crops share one commit. The building revision is an EF concurrency token.
            await context.SaveChangesAsync(cancellationToken);

            if (dirty.Count > 0 && (request.Building == HideoutBuildingEnum.None || request.Operation == HideoutOperationEnum.Read))
            {
                logger.LogInformation("Hideout timers advanced. CharacterId: {CharacterId}, Buildings: {BuildingCount}",
                    characterId, dirty.Count);
            }
        }

        var herbalismExperience = await context.CharacterExperiences
            .Where(x => x.CharacterId == characterId)
            .Where(x => x.Type == ExperienceTypeEnum.Herbalism)
            .SumAsync(x => x.Amount, cancellationToken);

        return new HideoutDto
        {
            Outcome = outcome,
            HerbalismLevel = ExperienceProgression.GetLevel(herbalismExperience),
            CurrentTime = now,
            Buildings = definitions
                .Select(definition =>
                {
                    var state = buildings
                        .Where(x => x.HideoutBuildingTypeId == definition.Id)
                        .SingleOrDefault();

                    var farm = state != null ? DeserializeState(state) as FarmState : null;

                    return new HideoutBuildingDto
                    {
                        Id = definition.Id,
                        Name = definition.Name,
                        Requirement = definition.Requirement,
                        BuildTime = definition.BuildTime,
                        BuildStartedAt = state?.BuildStartedAt,
                        BuildEndsAt = state?.BuildEndsAt,
                        Farm = farm,
                        UpgradeRequirement = definition.Id == HideoutBuildingEnum.Farm
                            ? FarmState.UpgradeRequirement(farm?.Level ?? 1) : new([], 0),
                        UpgradeTime = definition.Id == HideoutBuildingEnum.Farm ? FarmState.UpgradeTime(farm?.Level ?? 1) : 0
                    };
                })
                .ToArray(),
            CharacterInventory = new CharacterInventoryDto
            {
                CharacterId = characterId,
                Count = (short)Math.Max(inventory.Count, inventory.Inventory.Items.Count),
                Inventory = new InventoryDto
                {
                    Items = inventory.Inventory.Items
                        .Select(x => new InventoryItemDto { Type = x.Type, Count = x.Count })
                        .ToArray()
                }
            }
        };
    }

    private object? DeserializeState(CharacterHideout building) => building.HideoutBuildingTypeId switch
    {
        HideoutBuildingEnum.Farm => stateSerializer.Deserialize<FarmState>(building.Data),
        _ => null
    };

}
