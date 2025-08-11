using Microsoft.Extensions.Configuration;
using Orleans.Providers.RavenDb.StorageProviders;

[assembly: RegisterProvider("RavenDbGrainStorage", "GrainStorage", "Silo", typeof(Orleans.Providers.RavenDb.Hosting.RavenDbGrainStorageProviderBuilder))]

namespace Orleans.Providers.RavenDb.Hosting
{
    internal sealed class RavenDbGrainStorageProviderBuilder : IProviderBuilder<ISiloBuilder>
    {
        public void Configure(ISiloBuilder builder, string name, IConfigurationSection section)
        {
            builder.AddRavenDbGrainStorage(name, section.Bind);
        }
    }
}