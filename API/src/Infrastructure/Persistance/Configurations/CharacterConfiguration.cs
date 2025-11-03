using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder
            .Property(builder => builder.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder
            .Property(x => x.Level)
            .HasDefaultValue((byte)1);

        builder
            .HasOne(x => x.ApplicationUser)
            .WithMany(x => x.Characters)
            .HasForeignKey(x => x.ApplicationUserId);

        builder
            .HasOne(x => x.CharacterInventory)
            .WithOne(x => x.Character)
            .HasForeignKey<CharacterInventory>(x => x.Id);

        builder
            .HasMany(x => x.CharacterTransforms)
            .WithOne(x => x.Character)
            .HasForeignKey(x => x.CharacterId);

        builder
            .HasMany(x => x.CharacterExperiences)
            .WithOne(x => x.Character)
            .HasForeignKey(x => x.CharacterId);

        builder
            .HasMany(x => x.CharacterQuests)
            .WithOne(x => x.Character)
            .HasForeignKey(x => x.CharacterId);
    }
}
