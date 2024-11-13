using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Providers.RavenDB.StorageProviders;
using Orleans.TestingHost;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;

namespace UnitTests.Infrastructure;


public class RavenDbStorageFixture : BaseTestClusterFixture
{
    public IDocumentStore DocumentStore;

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
                    .AddRavenDbGrainStorage("GrainStorageForTest", options =>
                    {
                        options.DatabaseName = PersistenceGrainTests_RavenDBGrainStorage.TestDatabaseName;
                        options.Urls = [serverUrl];
                    })
                    .AddRavenDbGrainStorageAsDefault(options =>
                    {
                        options.DatabaseName = PersistenceGrainTests_RavenDBGrainStorage.TestDatabaseName;
                        options.Urls = [serverUrl];
                    })
                    .AddMemoryGrainStorage("MemoryStore");
            });
        }
    }

    public override Task InitializeAsync()
    {
        EmbeddedServer.Instance.StartServer();

        DocumentStore = EmbeddedServer.Instance.GetDocumentStore(PersistenceGrainTests_RavenDBGrainStorage.TestDatabaseName);

        return base.InitializeAsync();
    }

    public override Task DisposeAsync()
    {
        try
        {
            DocumentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(PersistenceGrainTests_RavenDBGrainStorage.TestDatabaseName, hardDelete: true));
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
