using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.Property(character => character.Name).IsRequired().HasMaxLength(100);

        builder
            .HasOne(character => character.CharacterInventory)
            .WithOne(inventory => inventory.Character)
            .HasForeignKey<CharacterInventory>(inventory => inventory.Id);

        builder
            .HasMany(character => character.CharacterTransforms)
            .WithOne(transform => transform.Character)
            .HasForeignKey(transform => transform.CharacterId);

        builder
            .HasMany(character => character.CharacterExperiences)
            .WithOne(experience => experience.Character)
            .HasForeignKey(experience => experience.CharacterId);

        builder
            .HasMany(character => character.CharacterQuests)
            .WithOne(characterQuest => characterQuest.Character)
            .HasForeignKey(characterQuest => characterQuest.CharacterId);
    }
}
