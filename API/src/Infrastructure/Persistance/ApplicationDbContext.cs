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

    public DbSet<HideoutBuildingType> HideoutBuildingTypes => Set<HideoutBuildingType>();
    public DbSet<CharacterHideout> CharacterHideouts => Set<CharacterHideout>();

    public DbSet<CharacterStash> CharacterStashes => Set<CharacterStash>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterSettings> CharacterSettings => Set<CharacterSettings>();
    public DbSet<CharacterFriendship> CharacterFriendships => Set<CharacterFriendship>();
    public DbSet<CharacterTransform> CharacterTransforms => Set<CharacterTransform>();
    public DbSet<CharacterExperience> CharacterExperiences => Set<CharacterExperience>();
    public DbSet<CharacterQuest> CharacterQuests => Set<CharacterQuest>();
    public DbSet<CharacterInventory> CharacterInventories => Set<CharacterInventory>();
    public DbSet<CharacterInventoryTradeReceipt> CharacterInventoryTradeReceipts => Set<CharacterInventoryTradeReceipt>();
    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<CraftingRecipe> CraftingRecipes => Set<CraftingRecipe>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AdvanceInventoryRevisions();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AdvanceInventoryRevisions();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AdvanceInventoryRevisions()
    {
        foreach (var entry in ChangeTracker.Entries<CharacterHideout>().Where(x => x.State == EntityState.Modified))
        {
            var revision = entry.Property(x => x.Revision);
            revision.CurrentValue = revision.OriginalValue + 1;
        }

        foreach (var entry in ChangeTracker.Entries<CharacterInventory>().Where(x => x.State == EntityState.Modified))
        {
            var revision = entry.Property(x => x.Revision);
            revision.CurrentValue = revision.OriginalValue + 1;
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
