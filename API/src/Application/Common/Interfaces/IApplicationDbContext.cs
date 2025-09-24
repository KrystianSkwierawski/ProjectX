using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.Common.Interfaces;
public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }

    DbSet<CharacterTransform> CharacterTransforms { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
