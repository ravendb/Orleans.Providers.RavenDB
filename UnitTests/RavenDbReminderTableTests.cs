using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Providers.RavenDB.Reminders;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;
using UnitTests;
using UnitTests.RemindersTest;
using Xunit;

//[Collection("RavenDbReminderTests")]
[TestCategory("Reminders")]

public class RavenDbReminderTableTests : ReminderTableTestsBase
{
    public RavenDbReminderTableTests(ConnectionStringFixture fixture, TestEnvironmentFixture clusterFixture)
        : base(fixture, clusterFixture, new LoggerFilterOptions())
    {
    }

    protected override Task<string> GetConnectionString()
    {
        return Task.FromResult(string.Empty);
    }

    protected override IReminderTable CreateRemindersTable()
    {
        // Start embedded RavenDB server

        EmbeddedServer.Instance.StartServer();
        var serverUrl = EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult().AbsoluteUri;

        // Set up RavenDB Reminder Table
        var options = new RavenDbReminderOptions
        {
            DatabaseName = "TestReminders",
            Urls = [serverUrl],
            WaitForIndexesAfterSaveChanges = true
        };

        return new RavenDbReminderTable(options, loggerFactory.CreateLogger<RavenDbReminderTable>());
    }

    [Fact]
    public async Task RavenDbReminderTable_TestRemindersRange()
    {
        await RemindersRange(50);
    }

    [Fact]
    public async Task RavenDbReminderTable_TestRemindersParallelUpsert()
    {
        await RemindersParallelUpsert();
    }

    [Fact]
    public async Task RavenDbReminderTable_TestReminderSimple()
    {
        await ReminderSimple();
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();

        // Clean up RavenDB resources
        (await EmbeddedServer.Instance.GetDocumentStoreAsync("TestReminders"))
            .Maintenance.Server.Send(new DeleteDatabasesOperation("TestReminders", hardDelete: true));

        EmbeddedServer.Instance.Dispose();
    }
}