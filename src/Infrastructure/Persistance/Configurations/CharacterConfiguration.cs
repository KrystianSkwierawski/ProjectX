using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance.Configurations;
public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        //builder
        //    .HasOne(c => c.ApplicationUser)
        //    .WithMany(u => u.Characters)
        //    .HasForeignKey(c => c.ApplicationUserId);
    }
}
