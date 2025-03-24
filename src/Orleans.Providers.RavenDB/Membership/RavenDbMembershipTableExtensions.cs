using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.RavenDb.Configuration;

namespace Orleans.Providers.RavenDb.Membership;

/// <summary>
/// Extension methods for configuring RavenDB as the membership table storage in Orleans.
/// </summary>
public static class RavenDbMembershipTableExtensions
{
    /// <summary>
    /// Configures RavenDB as the membership table provider for the Orleans silo.
    /// </summary>
    /// <param name="builder">The Orleans silo builder.</param>
    /// <param name="configureOptions">An action to configure <see cref="RavenDbMembershipOptions"/>.</param>
    /// <returns>The updated <see cref="ISiloBuilder"/> instance.</returns>
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