using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CharacterFriendshipConfiguration : IEntityTypeConfiguration<CharacterFriendship>
{
    public void Configure(EntityTypeBuilder<CharacterFriendship> builder)
    {
        builder.HasIndex(friendship => new { friendship.FirstCharacterId, friendship.SecondCharacterId }).IsUnique();
        builder.Property(friendship => friendship.Status).IsConcurrencyToken();

        builder
            .HasOne(friendship => friendship.FirstCharacter)
            .WithMany()
            .HasForeignKey(friendship => friendship.FirstCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(friendship => friendship.SecondCharacter)
            .WithMany()
            .HasForeignKey(friendship => friendship.SecondCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_CharacterFriendship_CharacterOrder",
                "[FirstCharacterId] < [SecondCharacterId]");
            table.HasCheckConstraint(
                "CK_CharacterFriendship_Requester",
                "[RequestedByCharacterId] = [FirstCharacterId] OR [RequestedByCharacterId] = [SecondCharacterId]");
        });
    }
}
