using AqueductCommon.Extensions;
using Microsoft.EntityFrameworkCore;
using MusicPipeBot.DbContexts;
using Npgsql;

namespace MusicPipeBot.Infrastructure;

public static class DbExtensions
{
    private const string DbConnectionPropertyName = "PostgreSQL";

    public static async Task WaitForDbConnection(this WebApplication app)
    {
        const int attemptsCount = 3;
        const int retryDelayInMilliseconds = 5000;

        await using var connection = new NpgsqlConnection(app.Configuration.GetConnectionString(DbConnectionPropertyName));

        for (var i = 0; i < attemptsCount; i++)
        {
            try
            {
                await connection.OpenAsync();
                app.Logger.Info("Successfully connected to the database");
                return;
            }
            catch (NpgsqlException e)
            {
                app.Logger.Warn(
                    "Failed to connect to the database. Attempt: {current}/{overall}. Message: {message}",
                    i + 1, attemptsCount, e.Message);
                Thread.Sleep(retryDelayInMilliseconds);
            }
        }

        throw new ApplicationException($"Failed to connect to the database after {attemptsCount} attempts");
    }

    public static async Task ApplyPendingMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var mainDbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        app.Logger.Info("Looking for any pending migrations...");
        var pendingMigrations = (await mainDbContext.Database.GetPendingMigrationsAsync()).ToList();
        if (pendingMigrations.Count > 0)
        {
            await mainDbContext.Database.MigrateAsync();
            app.Logger.Info($"Applied all migrations ({pendingMigrations.Count} overall)");
        }
    }
}