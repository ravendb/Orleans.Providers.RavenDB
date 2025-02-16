using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Reminders;
using TestExtensions;
using UnitTests;
using UnitTests.Infrastructure;
using UnitTests.RemindersTest;
using Xunit;
using Xunit.Abstractions;

[TestCategory("Reminders")]
public class RavenDbReminderTableTests : ReminderTableTestsBase, IClassFixture<RavenDbFixture>
{
    private readonly RavenDbFixture ravenDbFixture;

    public RavenDbReminderTableTests(ITestOutputHelper output, RavenDbFixture ravenDbFixture, ConnectionStringFixture fixture, TestEnvironmentFixture clusterFixture)
        : base(fixture, clusterFixture, new LoggerFilterOptions())
    {
        this.ravenDbFixture = ravenDbFixture;
    }

    protected override Task<string> GetConnectionString()
    {
        return Task.FromResult(string.Empty);
    }

    public override Task InitializeAsync()
    {
        Options.DatabaseName = ravenDbFixture.TestDatabaseName;
        Options.Urls = [RavenDbFixture.ServerUrl.AbsoluteUri];
        return base.InitializeAsync();
    }

    protected override IReminderTable CreateRemindersTable()
    {
        // Start embedded RavenDB server

        // Set up RavenDB Reminder Table
        Options = new RavenDbReminderOptions
        {
            WaitForIndexesAfterSaveChanges = true
        };

        return new RavenDbReminderTable(Options, loggerFactory.CreateLogger<RavenDbReminderTable>());
    }

    public RavenDbReminderOptions Options { get; set; }

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
}