using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;
using ProjectX.Domain.Enums;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CharacterSettingsConfiguration : IEntityTypeConfiguration<CharacterSettings>
{
    public void Configure(EntityTypeBuilder<CharacterSettings> builder)
    {
        builder.HasKey(x => x.CharacterId);

        builder.HasOne(x => x.Character).WithOne().HasForeignKey<CharacterSettings>(x => x.CharacterId);

        builder.Property(x => x.ActionBars)
            .HasMaxLength(256)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<InventoryItemEnum[]>(value, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(new ValueComparer<InventoryItemEnum[]>(
                (left, right) => left!.SequenceEqual(right!),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                value => value.ToArray()));
    }
}
