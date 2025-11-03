using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class CharacterInventoryConfiguration : IEntityTypeConfiguration<CharacterInventory>
{
    public void Configure(EntityTypeBuilder<CharacterInventory> builder)
    {
        builder
            .Property(x => x.Inventory)
            .IsRequired();

        builder
            .HasOne(x => x.Character)
            .WithOne(x => x.CharacterInventory)
            .HasForeignKey<CharacterInventory>(x => x.Id);
    }
}
