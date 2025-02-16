using Microsoft.Extensions.Hosting;
using Orleans.Providers.RavenDb.Membership;
using Orleans.TestingHost;
using Raven.Embedded;
using UnitTests.Grains;

namespace UnitTests.Infrastructure;


public class RavenDbMembershipFixture : RavenDbFixture
{
    //public IDocumentStore DocumentStore;

    public const string ClusterId = "TestCluster"; 

    //public const string TestDatabaseName = "TestMembership";

    protected override void ConfigureTestCluster(TestClusterBuilder builder)
    {
        builder.Options.InitialSilosCount = 2;
        builder.Properties["Database"] = TestDatabaseName;
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
                    options.DatabaseName = hostBuilder.GetConfigurationValue("Database");
                    options.ClusterId = ClusterId;
                    options.WaitForIndexesAfterSaveChanges = true;
                });
            });
        }
    }

    public override async Task DisposeAsync()
    {
        try
        {
            var grain = Client.GetGrain<IMembershipTestGrain>("Cleanup");
            await grain.DeleteMembershipTableEntries(ClusterId);

            if (HostedCluster != null)
                await HostedCluster.StopAllSilosAsync(); // Stop the cluster first
        }
        catch
        {
            // Ignored
        }

        await base.DisposeAsync();
    }
}

