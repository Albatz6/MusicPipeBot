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
        const int retryDelayInSeconds = 5000;

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
                Thread.Sleep(retryDelayInSeconds);
            }
        }

        throw new ApplicationException($"Failed to connect to the database after {attemptsCount} attempts");
    }

    public static async Task ApplyPendingMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userDbContext = scope.ServiceProvider.GetRequiredService<MainContext>();

        app.Logger.Info("Looking for any pending migrations...");
        var pendingMigrations = await userDbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await userDbContext.Database.MigrateAsync();
            app.Logger.Info("Applied all migrations");
        }
    }
}