using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Entities;

namespace Application.Common.Interfaces;
public interface IApplicationDbContext
{
    DbSet<Character> Character { get; }

    DbSet<CharacterPosition> CharacterPosition { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
