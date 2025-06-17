using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.RavenDb.Configuration;
using Raven.Client.Documents;

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

    /// <summary>
    /// Configures RavenDB as the membership table provider for the Orleans silo using an existing DocumentStore.
    /// </summary>
    /// <param name="builder">The Orleans silo builder.</param>
    /// <param name="documentStore">An existing initialized RavenDB <see cref="IDocumentStore"/> to be used by the membership provider.</param>
    /// <param name="configureOptions">An action to configure <see cref="RavenDbMembershipOptions"/>.</param>
    /// <returns>The updated <see cref="ISiloBuilder"/> instance.</returns>
    public static ISiloBuilder UseRavenDbMembershipTable(this ISiloBuilder builder, IDocumentStore documentStore, Action<RavenDbMembershipOptions>? configureOptions = null)
    {
        return builder.ConfigureServices(services =>
        {
            var options = new RavenDbMembershipOptions
            {
                DocumentStore = documentStore
            };

            configureOptions?.Invoke(options);

            services.AddSingleton(options);
            services.AddSingleton<IMembershipTable, RavenDbMembershipTable>();
        });
    }
}