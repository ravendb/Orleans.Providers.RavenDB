using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers.RavenDB.Configuration;

public static class RavenDbMembershipTableExtensions
{
    public static ISiloBuilder UseRavenDbMembershipTable(this ISiloBuilder builder, Action<RavenDbMembershipOptions> configureOptions)
    {
        return builder.ConfigureServices(services =>
        {
            var options = new RavenDbMembershipOptions();
            configureOptions(options);
            services.AddSingleton(options);
            services.AddSingleton<IMembershipTable, RavenDbMembershipTable>();
        });
    }
}