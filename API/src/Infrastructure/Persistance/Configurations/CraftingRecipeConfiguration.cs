using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CraftingRecipeConfiguration : IEntityTypeConfiguration<CraftingRecipe>
{
    public void Configure(EntityTypeBuilder<CraftingRecipe> builder)
    {
        builder
            .Property(x => x.Id)
            .ValueGeneratedNever();

        builder
            .HasIndex(x => x.Status)
            .HasDatabaseName("IX.CraftingRecipe.Status");
    }
}
