using Microsoft.Extensions.Hosting;
using Orleans.TestingHost;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;
using UnitTests.Grains;

namespace UnitTests.Infrastructure;


public class RavenDbMembershipFixture : BaseTestClusterFixture
{
    public IDocumentStore DocumentStore;

    public const string ClusterId = "TestCluster"; 

    public const string TestDatabaseName = "TestMembership";


    protected override void ConfigureTestCluster(TestClusterBuilder builder)
    {
        builder.Options.InitialSilosCount = 2; // For distributed testing

        builder.AddSiloBuilderConfigurator<SiloConfigurator>();

    }

    private class SiloConfigurator : IHostConfigurator
    {
        public void Configure(IHostBuilder hostBuilder)
        {
            var serverUrl = EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult().AbsoluteUri;

            hostBuilder.UseOrleans((_, siloBuilder) =>
            {
                siloBuilder.UseRavenDbMembershipTable(options =>
                {
                    options.Urls = [serverUrl];
                    options.DatabaseName = TestDatabaseName;
                    options.ClusterId = ClusterId;
                    options.WaitForIndexesAfterSaveChanges = true;
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

    public override async Task DisposeAsync()
    {
        try
        {
            var grain = Client.GetGrain<IMembershipTestGrain>("Cleanup");
            await grain.DeleteMembershipTableEntries(ClusterId);

            if (HostedCluster != null)
                await HostedCluster.StopAllSilosAsync(); // Stop the cluster first
            
            DocumentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(TestDatabaseName, hardDelete: true));
            DocumentStore.Dispose();
            EmbeddedServer.Instance.Dispose();
        }
        catch
        {
            // Ignored
        }

        await base.DisposeAsync();
    }
}

