using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Enums;

namespace ProjectX.Application.CraftingRecipes.Queries.GetCraftingRecipes;

public record GetCraftingRecipesQuery(CraftingRecipeTypeEnum type) : IRequest<GetCraftingRecipesDto>;

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
            .Where(x => x.Type == request.type)
            .Where(x => x.Status == StatusEnum.Active)
            .Select(x => new
            {
                x.Type,
                x.Requirement,
                x.Reward,
            })
            .ToListAsync(cancellationToken);

        return new GetCraftingRecipesDto
        {
            CraftingRecipes = craftingRecipes.Select(x => new CraftingRecipeDto
            {
                Requirement = JsonSerializer.Deserialize<CraftingRecipeRequirementDto>(x.Requirement)!,
                Reward = JsonSerializer.Deserialize<CraftingRecipeRewardDto>(x.Reward)!,
            }).ToList()
        };
    }
}