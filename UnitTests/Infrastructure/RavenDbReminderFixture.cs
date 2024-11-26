using Microsoft.Extensions.Hosting;
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
    //public IClusterClient ClusterClient;

    private const string TestDatabaseName = "OrleansReminders";

    //private const string TestDatabaseName = "OrleansReminders";

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
                    .AddRavenDbReminderTable(options =>
                    {
                        options.DatabaseName = TestDatabaseName;
                        options.Urls = new[] { serverUrl };
                    })
                    .AddMemoryGrainStorageAsDefault();
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
