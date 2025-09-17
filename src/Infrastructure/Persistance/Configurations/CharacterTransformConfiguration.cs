using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class CharacterTransformConfiguration : IEntityTypeConfiguration<CharacterTransform>
{
    public void Configure(EntityTypeBuilder<CharacterTransform> builder)
    {
    }
}
