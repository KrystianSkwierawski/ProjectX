using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class CharacterPositionConfiguration : IEntityTypeConfiguration<CharacterPosition>
{
    public void Configure(EntityTypeBuilder<CharacterPosition> builder)
    {
    }
}
