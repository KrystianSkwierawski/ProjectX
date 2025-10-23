using System.Transactions;
using Microsoft.EntityFrameworkCore;
using ProjectX.Domain.Entities;

namespace ProjectX.Application.Common.Interfaces;
public interface IApplicationDbContext
{
    DbSet<Character> Characters { get; }

    DbSet<CharacterTransform> CharacterTransforms { get; }

    DbSet<ProjectX.Domain.Entities.CharacterExperience> CharacterExperiences { get; }

    DbSet<CharacterQuest> CharacterQuests { get; }

    DbSet<CharacterInventory> CharacterInventories { get; }

    DbSet<Quest> Quests { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    TransactionScope CreateTransactionScope();
}
