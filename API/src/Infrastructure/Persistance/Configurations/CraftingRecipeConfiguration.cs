using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProjectX.Domain.Crafting;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;

public class CraftingRecipeConfiguration : IEntityTypeConfiguration<CraftingRecipe>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<CraftingRecipe> builder)
    {
        builder
            .Property(x => x.Id)
            .ValueGeneratedNever();

        ConfigureJsonProperty(builder.Property(x => x.Requirement));
        ConfigureJsonProperty(builder.Property(x => x.Reward));

        builder
            .HasIndex(x => new { x.Type, x.Status })
            .HasDatabaseName("IX.CraftingRecipe.Type.Status");
    }

    private static void ConfigureJsonProperty<T>(PropertyBuilder<T> propertyBuilder)
        where T : class
    {
        propertyBuilder
            .HasConversion(CreateConverter<T>())
            .Metadata.SetValueComparer(CreateComparer<T>());

        propertyBuilder.IsRequired();
    }

    private static ValueConverter<T, string> CreateConverter<T>()
        where T : class
    {
        return new(
            value => Serialize(value),
            value => Deserialize<T>(value));
    }

    private static ValueComparer<T> CreateComparer<T>()
        where T : class
    {
        return new(
            (left, right) => Serialize(left) == Serialize(right),
            value => Serialize(value).GetHashCode(StringComparison.Ordinal),
            value => Deserialize<T>(Serialize(value)));
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    private static T Deserialize<T>(string value)
    {
        return JsonSerializer.Deserialize<T>(value, SerializerOptions)
            ?? throw new InvalidOperationException($"Could not deserialize {typeof(T).Name}.");
    }
}
