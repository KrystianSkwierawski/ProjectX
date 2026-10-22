using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CharacterHideoutConfiguration : IEntityTypeConfiguration<CharacterHideout>
{
    public void Configure(EntityTypeBuilder<CharacterHideout> builder)
    {
        builder.Property(x => x.Data).IsRequired().HasDefaultValueSql("N'{}'");
        builder.Property(x => x.Revision).IsConcurrencyToken();

        builder.HasIndex(x => new { x.CharacterId, x.HideoutBuildingTypeId }).IsUnique();

        builder.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId);
        builder.HasOne(x => x.HideoutBuildingType).WithMany().HasForeignKey(x => x.HideoutBuildingTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
