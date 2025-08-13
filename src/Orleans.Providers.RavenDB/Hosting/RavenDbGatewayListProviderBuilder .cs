using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Messaging;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Membership;

[assembly: RegisterProvider("RavenDbGateway", "GatewayListProvider", "Client", typeof(Orleans.Providers.RavenDb.Hosting.RavenDbGatewayListProviderBuilder))]

namespace Orleans.Providers.RavenDb.Hosting;

internal sealed class RavenDbGatewayListProviderBuilder : IProviderBuilder<IClientBuilder>
{
    public void Configure(IClientBuilder builder, string? name, IConfigurationSection section)
    {
        builder.ConfigureServices(services =>
        {
            services.Configure<RavenDbMembershipOptions>(section);
            services.AddSingleton<IGatewayListProvider, RavenDbGatewayListProvider>();
        });
    }
}