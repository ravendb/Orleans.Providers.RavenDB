using Microsoft.Extensions.Configuration;
using Orleans.Providers.RavenDb.Hosting;
using Orleans.Providers.RavenDb.StorageProviders;

[assembly: RegisterProvider("RavenDBDatabase", "GrainStorage", "Silo", typeof(RavenDbDatabaseResourceGrainStorageProviderBuilder))]

namespace Orleans.Providers.RavenDb.Hosting
{
    internal sealed class RavenDbDatabaseResourceGrainStorageProviderBuilder : IProviderBuilder<ISiloBuilder>
    {
        public void Configure(ISiloBuilder builder, string? name, IConfigurationSection section)
        {
            RavenDbDatabaseResourceClusteringProviderBuilder.ConfigureSectionFromServiceKey(builder.Configuration, section);
            builder.AddRavenDbGrainStorage(name ?? string.Empty, section.Bind);
        }
    }
}