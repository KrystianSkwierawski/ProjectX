using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class HideoutBuildingTypeConfiguration : IEntityTypeConfiguration<HideoutBuildingType>
{
    public void Configure(EntityTypeBuilder<HideoutBuildingType> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();

        CraftingRecipeConfiguration.ConfigureJsonProperty(builder.Property(x => x.Requirement));
    }
}
