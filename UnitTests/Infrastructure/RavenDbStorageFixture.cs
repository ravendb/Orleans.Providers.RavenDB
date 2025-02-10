using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Providers.RavenDB;
using Orleans.Providers.RavenDB.StorageProviders;
using Orleans.Serialization;
using Orleans.Serialization.Codecs;
using Orleans.TestingHost;
using Raven.Embedded;
using UnitTests.Grains;

namespace UnitTests.Infrastructure;


public class RavenDbStorageFixture : RavenDbFixture
{
    public static RavenDbStorageFixture Instance { get; private set; }

    public DefaultTestHook TestHook { get; private set; } // Expose the hook as a property

    protected override string TestDatabaseName => DbName;

    private const string DbName = "TestStorage";

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
                        options.DatabaseName = DbName;
                        options.Urls = [serverUrl];
                    })
                    .AddRavenDbGrainStorageAsDefault(options =>
                    {
                        options.DatabaseName = DbName;
                        options.Urls = [serverUrl];
                    })
                    .AddMemoryGrainStorage("MemoryStore");
            });
        }
    }
}
