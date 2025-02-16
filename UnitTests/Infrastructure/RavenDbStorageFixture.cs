﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Orleans.Providers.RavenDB.StorageProviders;
using Orleans.TestingHost;
using Raven.Client.Documents;

namespace UnitTests.Infrastructure;


public class RavenDbStorageFixture : RavenDbFixture
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
            var databaseName = hostBuilder.GetConfigurationValue("Database");

            hostBuilder
                .ConfigureServices(services => services.AddSingleton(new DocumentStore
                {
                    Database = databaseName,
                    Urls = [ServerUrl.AbsoluteUri]
                }.Initialize()))
                .UseOrleans((_, siloBuilder) =>
                {
                    siloBuilder
                        .AddRavenDbGrainStorage("GrainStorageForTest", options =>
                        {
                            options.DatabaseName = databaseName;
                            options.Urls = [ServerUrl.AbsoluteUri];
                        })
                        .AddRavenDbGrainStorageAsDefault(options =>
                        {
                            options.DatabaseName = databaseName;
                            options.Urls = [ServerUrl.AbsoluteUri];
                        })
                        .AddMemoryGrainStorage("MemoryStore");
                });
        }
    }
}
