using Microsoft.EntityFrameworkCore;

namespace MusicPipeBot.DbContexts;

public class MainContext(IConfiguration configuration) : DbContext
{
    private const string DbConnectionPropertyName = "PostgreSQL";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder
            // .UseLazyLoadingProxies()
            .UseNpgsql(configuration.GetConnectionString(DbConnectionPropertyName));
}