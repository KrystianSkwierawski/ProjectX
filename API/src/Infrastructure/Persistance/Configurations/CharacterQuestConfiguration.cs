using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CharacterQuestConfiguration : IEntityTypeConfiguration<CharacterQuest>
{
    public void Configure(EntityTypeBuilder<CharacterQuest> builder)
    {
        builder.Property(x => x.Status).IsConcurrencyToken();

        builder.Property(x => x.ObjectiveProgress)
            .IsConcurrencyToken()
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<Dictionary<string, int>>(value, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, int>>(
                (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(),
                value => new Dictionary<string, int>(value)));

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
