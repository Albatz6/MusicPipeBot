using AqueductCommon.Models;
using Microsoft.EntityFrameworkCore;
using MusicPipeBot.Models;

namespace MusicPipeBot.DbContexts;

public class MainDbContext(IConfiguration configuration) : DbContext
{
    private const string DbConnectionPropertyName = "PostgreSQL";

    public DbSet<UserState> UserStates { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder
            .UseLazyLoadingProxies()
            .UseNpgsql(configuration.GetConnectionString(DbConnectionPropertyName));

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken token = default)
    {
        foreach (var entity in ChangeTracker
                     .Entries()
                     .Where(x => x is { Entity: BaseStoredModel, State: EntityState.Modified })
                     .Select(x => x.Entity)
                     .Cast<BaseStoredModel>())
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, token);
    }
}