using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Entities;

namespace Application.Common.Interfaces;
public interface IApplicationDbContext
{
    DbSet<Player> Players { get; }

    DbSet<PlayerPosition> PlayerPositions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
