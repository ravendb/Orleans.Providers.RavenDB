using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans;
using Orleans.TestingHost;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;

namespace UnitTests.Infrastructure;


public class RavenDbMembershipFixture : BaseTestClusterFixture
{
    public IDocumentStore DocumentStore;

    private const string TestDatabaseName = "TestMembership";


    protected override void ConfigureTestCluster(TestClusterBuilder builder)
    {
        builder.Options.InitialSilosCount = 2; // For distributed testing

        builder.AddSiloBuilderConfigurator<SiloConfigurator>();

        builder.AddClientBuilderConfigurator<ClientConfigurator>();

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
                });
            });
        }
    }

    private class ClientConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IMembershipTable, RavenDbMembershipTable>();
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
        }
        catch
        {
            // Ignored
        }

        return base.DisposeAsync();
    }
}

