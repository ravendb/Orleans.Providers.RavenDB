using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.RavenDb.Reminders;
using Orleans.TestingHost;
using Raven.Embedded;

namespace UnitTests.Infrastructure;


public class RavenDbReminderFixture : RavenDbFixture
{
    protected override void ConfigureTestCluster(TestClusterBuilder builder)
    {
        builder.Options.InitialSilosCount = 1;
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
                siloBuilder
                    .Configure<GrainCollectionOptions>(options =>
                    {
                        options.DeactivationTimeout = TimeSpan.FromMinutes(10); // Prevent early deactivation
                    })
                    .AddRavenDbReminderTable(options =>
                    {
                        options.DatabaseName = hostBuilder.GetConfigurationValue("Database");
                        options.Urls = new[] { serverUrl };
                        options.WaitForIndexesAfterSaveChanges = true;
                    })
                    .AddMemoryGrainStorageAsDefault()
                    .Configure<ReminderOptions>(options =>
                    {
                        // Set a lower minimum reminder period for testing
                        options.MinimumReminderPeriod = TimeSpan.FromSeconds(5);
                    });
            });
        }
    }
}
