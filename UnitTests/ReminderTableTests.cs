using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.RavenDB.Reminders;
using Orleans.Runtime;
using Orleans.TestingHost;
using Raven.Client.Documents;
using Raven.Embedded;
using UnitTests.Grains;
using UnitTests.Infrastructure;
using Xunit;
using static ReminderTableTests;

public class ReminderTableTests : IClassFixture<RavenDbReminderFixture>
{
    private readonly RavenDbReminderFixture _fixture;

    public ReminderTableTests(RavenDbReminderFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test_ReminderRegistration()
    {
        var testGrainId = 1;
        var reminderName = "test-reminder";

        var grain = _fixture.ClusterClient.GetGrain<IReminderGrainForTesting>(testGrainId);
        await grain.AddReminder(reminderName);

        var exists = await grain.IsReminderExists(reminderName);
        Assert.True(exists);
    }

    [Fact]
    public async Task Test_ReadRows_RangeBoundaries()
    {
        var grainId1 = GrainId.Create("test", "grain1");
        var grainId2 = GrainId.Create("test", "grain2");

        var reminder1 = new ReminderEntry
        {
            GrainId = grainId1,
            ReminderName = "test1",
            StartAt = DateTime.UtcNow,
            Period = TimeSpan.FromMinutes(1)
        };

        var reminder2 = new ReminderEntry
        {
            GrainId = grainId2,
            ReminderName = "test2",
            StartAt = DateTime.UtcNow,
            Period = TimeSpan.FromMinutes(2)
        };

        var reminderTableGrain = _fixture.ClusterClient.GetGrain<IReminderGrainForTesting>(grainId1);

        // Use the new helper methods to upsert and read reminders
        await reminderTableGrain.UpsertReminder(reminder1);
        await reminderTableGrain.UpsertReminder(reminder2);

        var result = await reminderTableGrain.ReadRowsInRange(0, uint.MaxValue);

        Assert.Contains(result.Reminders, r => r.GrainId == grainId1);
        Assert.Contains(result.Reminders, r => r.GrainId == grainId2);
    }



    public class RavenDbReminderFixture : RavenDbStorageFixture
    {
        public IDocumentStore DocumentStore;
        public IClusterClient ClusterClient;

        private const string TestDatabaseName = "OrleansReminders";

        protected override void ConfigureTestCluster(TestClusterBuilder builder)
        {
            builder.Options.InitialSilosCount = 1;
            builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        }

        private class SiloConfigurator : IHostConfigurator
        {
            public void Configure(IHostBuilder hostBuilder)
            {
                var serverUrl = EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult().AbsoluteUri;

                hostBuilder.UseOrleans((_, siloBuilder) =>
                {
                    siloBuilder
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "test-cluster";
                            options.ServiceId = "ReminderTestService";
                        })
                        .AddRavenDbReminderTable(options =>
                        {
                            options.DatabaseName = TestDatabaseName;
                            options.Urls = new[] { serverUrl };
                        })
                        .AddMemoryGrainStorageAsDefault();
                });
            }
        }
    }
}



