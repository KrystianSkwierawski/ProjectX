using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class CharacterExperienceConfiguration : IEntityTypeConfiguration<CharacterExperience>
{
    public void Configure(EntityTypeBuilder<CharacterExperience> builder)
    {
        builder
            .HasOne(x => x.Character)
            .WithMany(x => x.CharacterExperiences)
            .HasForeignKey(x => x.CharacterId);
    }
}
