using Microsoft.Extensions.Configuration;
using Orleans.Providers.RavenDb.Hosting;
using Orleans.Providers.RavenDb.Membership;

[assembly: RegisterProvider("RavenDbClustering", "Clustering", "Silo", typeof(RavenDbClusteringProviderBuilder))]

namespace Orleans.Providers.RavenDb.Hosting
{
    internal sealed class RavenDbClusteringProviderBuilder : IProviderBuilder<ISiloBuilder>
    {
        public void Configure(ISiloBuilder builder, string? name, IConfigurationSection section)
        {
            builder.UseRavenDbMembershipTable(section.Bind);
        }
    }
}