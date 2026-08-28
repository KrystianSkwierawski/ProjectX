using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }
    DbSet<CharacterFriendship> CharacterFriendships { get; }
    DbSet<CharacterTransform> CharacterTransforms { get; }
    DbSet<CharacterExperience> CharacterExperiences { get; }
    DbSet<CharacterQuest> CharacterQuests { get; }
    DbSet<CharacterInventory> CharacterInventories { get; }
    DbSet<Quest> Quests { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<CraftingRecipe> CraftingRecipes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
