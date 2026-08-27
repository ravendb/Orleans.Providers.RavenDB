using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Orleans.Providers.RavenDb.Configuration;

internal static class RavenDbDatabaseInitializer
{
    private const int MaximumAttempts = 100;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    public static async Task EnsureDatabaseExistsAsync(
        IDocumentStore documentStore,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                var database = await documentStore.Maintenance.Server.SendAsync(
                    new GetDatabaseRecordOperation(databaseName),
                    cancellationToken);

                if (database is null)
                {
                    await documentStore.Maintenance.Server.SendAsync(
                        new CreateDatabaseOperation(new DatabaseRecord(databaseName)),
                        cancellationToken);
                }

                // A database record becomes visible before RavenDB finishes loading the database.
                // Probe it before providers create indexes or open sessions so concurrent silo startup is safe.
                await documentStore.Maintenance.ForDatabase(databaseName).SendAsync(
                    new GetStatisticsOperation(),
                    cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < MaximumAttempts && IsStartupRace(exception))
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }

    private static bool IsStartupRace(Exception exception) =>
        exception is ConcurrencyException or DatabaseDisabledException or DatabaseDoesNotExistException;
}
