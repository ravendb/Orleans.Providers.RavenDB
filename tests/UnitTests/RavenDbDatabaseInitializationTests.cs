using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Membership;
using Orleans.Providers.RavenDb.Reminders;
using Raven.Client.ServerWide.Operations;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests;

[TestCategory("Membership")]
public class RavenDbDatabaseInitializationTests : IClassFixture<RavenDbFixture>
{
    private readonly RavenDbFixture _fixture;

    public RavenDbDatabaseInitializationTests(RavenDbFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Membership, reminders and grain storage each ensure the database exists during silo
    /// startup. Starting them together against a database that does not yet exist is the
    /// ordinary first-run case, and it must not fail.
    /// </summary>
    [Fact]
    public async Task Providers_CanStartConcurrentlyAgainstMissingDatabase()
    {
        var databaseName = "OrleansConcurrentInit-" + Guid.NewGuid();
        var urls = new[] { RavenDbFixture.ServerUrl.AbsoluteUri };

        // A silo starts three providers against one database, and several silos may start at
        // once. The starters below stand in for that: each independently ensures the database
        // exists, which is exactly the window the providers contend over.
        var starters = new List<Func<Task>>();
        for (var i = 0; i < 4; i++)
        {
            IMembershipTable membership = new RavenDbMembershipTable(
                new RavenDbMembershipOptions
                {
                    Urls = urls,
                    DatabaseName = databaseName,
                    ClusterId = "concurrent-init-" + i,
                    EnsureDatabaseExists = true
                },
                NullLogger<RavenDbMembershipTable>.Instance);

            IReminderTable reminders = new RavenDbReminderTable(
                new RavenDbReminderOptions
                {
                    Urls = urls,
                    DatabaseName = databaseName,
                    EnsureDatabaseExists = true
                },
                NullLogger<RavenDbReminderTable>.Instance);

            starters.Add(() => membership.InitializeMembershipTable(true));
            starters.Add(() => reminders.StartAsync(CancellationToken.None));
        }

        try
        {
            // Release them together so none observes the database in a settled state.
            using var gate = new SemaphoreSlim(0, starters.Count);
            var running = starters
                .Select(start => Task.Run(async () =>
                {
                    await gate.WaitAsync();
                    await start();
                }))
                .ToArray();

            gate.Release(starters.Count);
            await Task.WhenAll(running);
        }
        finally
        {
            try
            {
                _fixture.DocumentStore.Maintenance.Server.Send(
                    new DeleteDatabasesOperation(databaseName, hardDelete: true));
            }
            catch
            {
                // The database may never have been created; nothing to clean up.
            }
        }
    }
}
