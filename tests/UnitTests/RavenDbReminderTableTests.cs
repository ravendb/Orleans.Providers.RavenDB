using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Reminders;
using Orleans.Runtime;
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

    [Fact]
    public async Task RavenDbReminderTable_ReadRowsByGrainId_ReturnsUsableETag()
    {
        IReminderTable table = new RavenDbReminderTable(Options, loggerFactory.CreateLogger<RavenDbReminderTable>());
        await table.StartAsync(CancellationToken.None);

        var grainId = GrainId.Create("reminder-etag-test", Guid.NewGuid().ToString("N"));
        await table.UpsertRow(new ReminderEntry
        {
            GrainId = grainId,
            ReminderName = "read-rows-etag",
            StartAt = DateTime.UtcNow,
            Period = TimeSpan.FromMinutes(1)
        });

        var rows = await table.ReadRows(grainId);
        var row = Assert.Single(rows.Reminders);

        // ReadRows(uint, uint) and ReadRow(GrainId, string) both return the change vector, and
        // Orleans treats ETag as the concurrency token. Asserting that a removal driven by this
        // overload's token succeeds proves the token is genuinely usable, not merely non-empty.
        Assert.False(string.IsNullOrEmpty(row.ETag));
        Assert.True(await table.RemoveRow(grainId, row.ReminderName, row.ETag));
    }
}