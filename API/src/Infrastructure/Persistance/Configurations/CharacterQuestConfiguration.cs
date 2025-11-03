using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class CharacterQuestConfiguration : IEntityTypeConfiguration<CharacterQuest>
{
    public void Configure(EntityTypeBuilder<CharacterQuest> builder)
    {
        builder
            .HasOne(x => x.Character)
            .WithMany(x => x.CharacterQuests)
            .HasForeignKey(x => x.CharacterId);

        builder
            .HasOne(x => x.Quest)
            .WithMany(x => x.CharacterQuests)
            .HasForeignKey(x => x.QuestId);
    }
}
