using System.Reflection;
using System.Transactions;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProjectX.Application.Common.Interfaces;
using ProjectX.Domain.Entities;

namespace ProjectX.Infrastructure.Persistance;

public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions) { }

    public DbSet<Character> Characters => Set<Character>();

    public DbSet<CharacterTransform> CharacterTransforms => Set<CharacterTransform>();

    public DbSet<CharacterExperience> CharacterExperiences => Set<CharacterExperience>();

    public DbSet<CharacterQuest> CharacterQuests => Set<CharacterQuest>();

    public DbSet<Quest> Quests => Set<Quest>();

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public TransactionScope CreateTransactionScope()
    {
        var options = new TransactionOptions
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
        };

        return new TransactionScope(TransactionScopeOption.Required, options, TransactionScopeAsyncFlowOption.Enabled);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
