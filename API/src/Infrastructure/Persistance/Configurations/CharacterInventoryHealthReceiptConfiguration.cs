using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public sealed class CharacterInventoryHealthReceiptConfiguration : IEntityTypeConfiguration<CharacterInventoryHealthReceipt>
{
    public void Configure(EntityTypeBuilder<CharacterInventoryHealthReceipt> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RequestFingerprint).HasMaxLength(64).IsFixedLength();

        builder
            .HasOne<Character>()
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
