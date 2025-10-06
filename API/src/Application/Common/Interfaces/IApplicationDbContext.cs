using System.Transactions;
using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.Common.Interfaces;
public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }

    DbSet<CharacterTransform> CharacterTransforms { get; }

    DbSet<ProjectX.Domain.Entities.CharacterExperience> CharacterExperiences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    TransactionScope CreateTransactionScope();
}
