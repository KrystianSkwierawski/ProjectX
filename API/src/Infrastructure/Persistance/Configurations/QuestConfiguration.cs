using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class QuestConfiguration : IEntityTypeConfiguration<Quest>
{
    public void Configure(EntityTypeBuilder<Quest> builder)
    {
        builder
            .Property(x => x.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(x => x.Description)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(x => x.CompleteDescription)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(x => x.StatusText)
            .HasMaxLength(255)
            .IsRequired();

        builder
            .Property(x => x.GameObjectName)
            .HasMaxLength(255);

        builder
            .HasMany(x => x.CharacterQuests)
            .WithOne(x => x.Quest)
            .HasForeignKey(x => x.QuestId);
    }
}
