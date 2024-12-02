using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.RavenDB.Reminders;
using Orleans.TestingHost;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;

namespace UnitTests.Infrastructure;


public class RavenDbReminderFixture : BaseTestClusterFixture
{
    public IDocumentStore DocumentStore;

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
                    //.UseLocalhostClustering()
                    //.Configure<ClusterOptions>(options =>
                    //{
                    //    options.ClusterId = "test-cluster";
                    //    options.ServiceId = "ReminderTestService";
                    //})
                    .Configure<GrainCollectionOptions>(options =>
                    {
                        options.DeactivationTimeout = TimeSpan.FromMinutes(10); // Prevent early deactivation
                    })
                    .AddRavenDbReminderTable(options =>
                    {
                        options.DatabaseName = TestDatabaseName;
                        options.Urls = new[] { serverUrl };
                    })
                    .AddMemoryGrainStorageAsDefault()
                    .Configure<ReminderOptions>(options =>
                    {
                        // Set a lower minimum reminder period for testing
                        options.MinimumReminderPeriod = TimeSpan.FromSeconds(5);
                    });
            });
        }
    }

    public override Task InitializeAsync()
    {
        EmbeddedServer.Instance.StartServer();

        DocumentStore = EmbeddedServer.Instance.GetDocumentStore(TestDatabaseName);

        return base.InitializeAsync();
    }

    public override Task DisposeAsync()
    {
        try
        {
            DocumentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(TestDatabaseName, hardDelete: true));
            DocumentStore.Dispose();

            EmbeddedServer.Instance.Dispose();

            return base.DisposeAsync();
        }
        catch
        {
            return Task.CompletedTask;
        }
    }
}
