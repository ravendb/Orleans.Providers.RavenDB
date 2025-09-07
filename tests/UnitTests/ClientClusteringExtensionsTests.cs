using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Messaging;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Membership;
using Raven.Embedded;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests
{
    public class ClientClusteringExtensionsTests : IClassFixture<RavenDbMembershipFixture>
    {
        private readonly RavenDbMembershipFixture _fixture;

        public ClientClusteringExtensionsTests(RavenDbMembershipFixture fixture) => _fixture = fixture;

        private static string GetServerUrl() => EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult().AbsoluteUri;

        [Fact]
        public void UseRavenDbClustering_ShouldRegisterProvider_AndBindOptions()
        {
            // Arrange DI
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();

            var configuration = new ConfigurationBuilder().Build();

            // Use the Orleans client builder only to register services
            var builder = new ClientBuilder(services, configuration)
                .ConfigureServices(s => s.Configure<ClusterOptions>(o =>
                {
                    o.ClusterId = RavenDbMembershipFixture.ClusterId;
                    o.ServiceId = "TestService";
                }))
                .UseRavenDbClustering(o =>
                {
                    o.Urls = new[] { GetServerUrl() };
                    o.DatabaseName = _fixture.TestDatabaseName;
                    o.ClusterId = RavenDbMembershipFixture.ClusterId;
                });

            // Build a ServiceProvider (no client Build/Connect needed)
            var sp = services.BuildServiceProvider();

            // Assert: provider registered + options bound
            var provider = sp.GetRequiredService<IGatewayListProvider>();
            var opts = sp.GetRequiredService<IOptions<RavenDbMembershipOptions>>().Value;

            Assert.IsType<RavenDbGatewayListProvider>(provider);
            Assert.Equal(RavenDbMembershipFixture.ClusterId, opts.ClusterId);
            Assert.Equal(_fixture.TestDatabaseName, opts.DatabaseName);
            Assert.Contains(GetServerUrl(), opts.Urls);
        }

        [Fact]
        public async Task GatewayProvider_ShouldReturnActiveGateways_FromRavenDb()
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();

            var configuration = new ConfigurationBuilder().Build();

            var builder = new ClientBuilder(services, configuration)
                .ConfigureServices(s => s.Configure<ClusterOptions>(o =>
                {
                    o.ClusterId = RavenDbMembershipFixture.ClusterId;
                    o.ServiceId = "TestService";
                }))
                .UseRavenDbClustering(o =>
                {
                    o.Urls = new[] { GetServerUrl() };
                    o.DatabaseName = _fixture.TestDatabaseName;
                    o.ClusterId = RavenDbMembershipFixture.ClusterId;
                });

            var sp = services.BuildServiceProvider();
            var provider = (RavenDbGatewayListProvider)sp.GetRequiredService<IGatewayListProvider>();

            await provider.InitializeGatewayListProvider();
            var gateways = await provider.GetGateways();

            Assert.NotNull(gateways);
            Assert.Equal(2, gateways.Count);
        }
    }
}
