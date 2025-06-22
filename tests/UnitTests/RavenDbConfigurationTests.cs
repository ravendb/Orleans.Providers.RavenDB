using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Membership;
using Orleans.Providers.RavenDb.StorageProviders;
using Raven.Client.Documents;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests
{
    public class RavenDbConfigurationTests
    {
        [Fact]
        public void CanInjectCustomDocumentStoreIntoMembershipOptions()
        {
            var documentStore = new DocumentStore
            {
                Urls = new[] { RavenDbFixture.ServerUrl.AbsoluteUri },
                Database = "TestDb"
            };
            documentStore.Initialize();

            var builder = new HostBuilder()
                .UseOrleans(silo =>
                {
                    silo.UseRavenDbMembershipTable(documentStore);
                });

            using var host = builder.Build();
            var options = host.Services.GetRequiredService<RavenDbMembershipOptions>();

            Assert.Equal(documentStore, options.DocumentStore);
        }

        [Fact]
        public void CanInjectCustomDocumentStoreIntoGrainStorageOptions()
        {
            var documentStore = new DocumentStore
            {
                Urls = new[] { RavenDbFixture.ServerUrl.AbsoluteUri },
                Database = "TestDb"
            };
            documentStore.Initialize();

            var providerName = "MyCustomGrainStorage";

            var builder = new HostBuilder()
                .UseOrleans(silo =>
                {
                    silo.AddRavenDbGrainStorage(providerName, documentStore);
                });

            using var host = builder.Build();

            var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<RavenDbGrainStorageOptions>>();
            var options = optionsMonitor.Get(providerName);

            Assert.Equal(documentStore, options.DocumentStore);
        }
    }
    }
