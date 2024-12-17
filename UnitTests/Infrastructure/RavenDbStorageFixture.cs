using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Providers.RavenDB;
using Orleans.Providers.RavenDB.StorageProviders;
using Orleans.Serialization;
using Orleans.Serialization.Codecs;
using Orleans.TestingHost;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;
using UnitTests.Grains;

namespace UnitTests.Infrastructure;


public class RavenDbStorageFixture : BaseTestClusterFixture
{
    public IDocumentStore DocumentStore;

    public static RavenDbStorageFixture Instance { get; private set; }

    public DefaultTestHook TestHook { get; private set; } // Expose the hook as a property

    //protected virtual string TestDatabase => "StorageTestsDatabase";

    public RavenDbStorageFixture()
    {
        Instance = this; // Set the static instance
    }

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
            hostBuilder.ConfigureServices(services =>
            {
                // Register the custom serializer
                services.AddSingleton<IFieldCodec<Raven.Client.Exceptions.ConcurrencyException>, ConcurrencyExceptionCodec>();

                // Access the static fixture instance
                var fixture = RavenDbStorageFixture.Instance;
                var testHook = new DefaultTestHook();
                fixture.TestHook = testHook; // Assign the hook to the fixture

                services.AddSingleton<ITestHook>(testHook); // Register the hook

                services.AddSerializer(builder => builder.AddAssembly(typeof(ConcurrencyExceptionCodec).Assembly));

            })
            .UseOrleans((_, siloBuilder) =>
            {
                siloBuilder
                    .AddRavenDbGrainStorage("GrainStorageForTest", options =>
                    {
                        options.DatabaseName = RavenDbPersistenceGrainTests.TestDatabaseName;
                        options.Urls = [serverUrl];
                    })
                    .AddRavenDbGrainStorageAsDefault(options =>
                    {
                        options.DatabaseName = RavenDbPersistenceGrainTests.TestDatabaseName;
                        options.Urls = [serverUrl];
                    })
                    .AddMemoryGrainStorage("MemoryStore");
            });
        }
    }

    public override Task InitializeAsync()
    {
        EmbeddedServer.Instance.StartServer();

        DocumentStore = EmbeddedServer.Instance.GetDocumentStore(RavenDbPersistenceGrainTests.TestDatabaseName);

        return base.InitializeAsync();
    }

    public override Task DisposeAsync()
    {
        try
        {
            DocumentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(RavenDbPersistenceGrainTests.TestDatabaseName, hardDelete: true));
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
