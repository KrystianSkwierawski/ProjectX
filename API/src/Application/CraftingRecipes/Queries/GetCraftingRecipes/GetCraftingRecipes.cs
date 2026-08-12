using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.CharacterInventories.Queries.GetCharacterInventory;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;

public record GetCraftingRecipesQuery(CraftingRecipeTypeEnum Type) : IRequest<GetCraftingRecipesDto>;

public class CraftingRecipesQueryHandler : IRequestHandler<GetCraftingRecipesQuery, GetCraftingRecipesDto>
{
    private readonly IApplicationDbContext _context;

    public CraftingRecipesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetCraftingRecipesDto> Handle(GetCraftingRecipesQuery request, CancellationToken cancellationToken)
    {
        var craftingRecipes = await _context.CraftingRecipes
            .Where(recipe => recipe.Type == request.Type)
            .Where(recipe => recipe.Status == StatusEnum.Active)
            .OrderBy(recipe => recipe.Id)
            .Select(recipe => new
            {
                recipe.Id,
                recipe.Requirement,
                recipe.Reward
            })
            .ToListAsync(cancellationToken);

        return new GetCraftingRecipesDto
        {
            CraftingRecipes = craftingRecipes.Select(recipe => new CraftingRecipeDto
            {
                Id = recipe.Id,
                Requirement = new CraftingRecipeRequirementDto
                {
                    Items = recipe.Requirement.Items
                        .Select(item => new InventoryItemDto
                        {
                            Type = item.Type,
                            Count = item.Count
                        })
                        .ToArray(),
                    Level = recipe.Requirement.Level
                },
                Reward = new CraftingRecipeRewardDto
                {
                    Item = new InventoryItemDto
                    {
                        Type = recipe.Reward.Item.Type,
                        Count = recipe.Reward.Item.Count
                    },
                    Experience = recipe.Reward.Experience
                }
            }).ToArray()
        };
    }
}
