using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Messaging;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Hosting;
using Orleans.Providers.RavenDb.Membership;

[assembly: RegisterProvider("RavenDBDatabase", "Clustering", "Client", typeof(RavenDbDatabaseResourceGatewayListProviderBuilder))]

namespace Orleans.Providers.RavenDb.Hosting;

internal sealed class RavenDbDatabaseResourceGatewayListProviderBuilder : IProviderBuilder<IClientBuilder>
{
    public void Configure(IClientBuilder builder, string? name, IConfigurationSection section)
    {
        RavenDbDatabaseResourceClusteringProviderBuilder.ConfigureSectionFromServiceKey(builder.Configuration, section);
        builder.ConfigureServices(services =>
        {
            services.Configure<RavenDbMembershipOptions>(section);
            services.AddSingleton<IGatewayListProvider, RavenDbGatewayListProvider>();
        });
    }
}