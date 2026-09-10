using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public sealed class CharacterInventoryTradeReceiptConfiguration : IEntityTypeConfiguration<CharacterInventoryTradeReceipt>
{
    public void Configure(EntityTypeBuilder<CharacterInventoryTradeReceipt> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RequestFingerprint).HasMaxLength(64).IsFixedLength();

        builder
            .HasOne<Character>()
            .WithMany()
            .HasForeignKey(x => x.SourceCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Character>()
            .WithMany()
            .HasForeignKey(x => x.TargetCharacterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
