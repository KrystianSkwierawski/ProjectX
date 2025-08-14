using Microsoft.EntityFrameworkCore;
using ProjectX.Model.DbSets;

namespace ProjectX.Model;
public class ProjectXContext : DbContext
{
    private readonly string _connectionString;

    public ProjectXContext() : base()
    {
        _connectionString ??= "Server=localhost;Database=ProjectX;Trusted_Connection=true;TrustServerCertificate=true;";
    }

    public ProjectXContext(string connectionString) : base()
    {
        _connectionString = connectionString;
    }

    public ProjectXContext(DbContextOptions<ProjectXContext> options) : base(options)
    {
    }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerPosition> PlayerPositions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
