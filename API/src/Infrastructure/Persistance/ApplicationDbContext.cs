using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;
using ProjectX.Infrastructure.Identity;

namespace ProjectX.Infrastructure.Persistance;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterFriendship> CharacterFriendships => Set<CharacterFriendship>();
    public DbSet<CharacterTransform> CharacterTransforms => Set<CharacterTransform>();
    public DbSet<CharacterExperience> CharacterExperiences => Set<CharacterExperience>();
    public DbSet<CharacterQuest> CharacterQuests => Set<CharacterQuest>();
    public DbSet<CharacterInventory> CharacterInventories => Set<CharacterInventory>();
    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<CraftingRecipe> CraftingRecipes => Set<CraftingRecipe>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
