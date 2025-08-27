using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class PlayerPositionConfiguration : IEntityTypeConfiguration<PlayerPosition>
{
    public void Configure(EntityTypeBuilder<PlayerPosition> builder)
    {
    }
}
