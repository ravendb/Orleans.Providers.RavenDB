using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers.RavenDb.StorageProviders;
using Orleans.Runtime;
using Orleans.TestingHost;
using Raven.Client.Documents;
using Sparrow.Json;
using UnitTests.Grains;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests
{
    public class RavenDbStorageSeparatorFixtureTests : IClassFixture<RavenDbStorageSeparatorFixture>
    {
        private readonly RavenDbStorageSeparatorFixture _fixture;

        public RavenDbStorageSeparatorFixtureTests(RavenDbStorageSeparatorFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GrainStorage_UsesConfigured_GrainKeySeparator()
        {
            var primaryKey = 982_734_551;
            var grain = _fixture.Client.GetGrain<ICounterGrain>(primaryKey);
            await grain.Increment();

            var separator = RavenDbStorageSeparatorFixture.Separator;
            var grainStateType = nameof(CounterGrainState);
            var grainId = grain.GetGrainId();
            var expectedId = $"{grainStateType}{separator}{grainId}";

            using var session = _fixture.DocumentStore.OpenAsyncSession();

            var doc = (await session.Advanced.LoadStartingWithAsync<object>(nameof(CounterGrainState))).FirstOrDefault();
            Assert.NotNull(doc);
            
            var docId = session.Advanced.GetDocumentId(doc);
            Assert.Equal(expectedId, docId);

        }

    }

    public sealed class RavenDbStorageSeparatorFixture : RavenDbFixture
    {
        public const string Separator = "_";    

        protected override void ConfigureTestCluster(TestClusterBuilder builder)
        {
            builder.Options.InitialSilosCount = 1;
            builder.Properties["Database"] = TestDatabaseName;
            builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        }

        private sealed class SiloConfigurator : IHostConfigurator
        {
            public void Configure(IHostBuilder hostBuilder)
            {
                var databaseName = hostBuilder.GetConfigurationValue("Database");

                hostBuilder
                    .ConfigureServices(services => services.AddSingleton(new DocumentStore
                    {
                        Database = databaseName,
                        Urls = [ServerUrl.AbsoluteUri]
                    }.Initialize()))
                    .UseOrleans((_, siloBuilder) =>
                    {
                        siloBuilder.AddRavenDbGrainStorageAsDefault(options =>
                        {
                            options.DatabaseName = databaseName;
                            options.Urls = [ServerUrl.AbsoluteUri];
                            options.GrainKeySeparator = Separator;
                        });
                    });
            }
        }
    }
}