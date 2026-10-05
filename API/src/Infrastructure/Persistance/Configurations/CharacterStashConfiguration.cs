using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;
using ProjectX.Infrastructure.Identity;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CharacterStashConfiguration : IEntityTypeConfiguration<CharacterStash>
{
    public void Configure(EntityTypeBuilder<CharacterStash> builder)
    {
        builder.HasKey(x => x.ApplicationUserId);
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<CharacterStash>(x => x.ApplicationUserId);

        builder.Property(x => x.Inventory)
            .HasConversion(CharacterInventoryConfiguration.CreateConverter())
            .Metadata.SetValueComparer(CharacterInventoryConfiguration.CreateComparer());

        builder.Property(x => x.Inventory).IsRequired();
        builder.Property(x => x.Revision).IsConcurrencyToken();
        builder.Property(x => x.Count).HasDefaultValue((short)64);
    }
}
