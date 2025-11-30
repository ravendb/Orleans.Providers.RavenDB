using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Messaging;
using Orleans.Providers.RavenDb.Configuration;

namespace Orleans.Providers.RavenDb.Membership
{
    public static class RavenDbClientClusteringExtensions
    {
        /// <summary>
        /// Configure Orleans client clustering to discover gateways via RavenDB using code-based options.
        /// </summary>
        public static IClientBuilder UseRavenDbClustering(
            this IClientBuilder builder,
            Action<RavenDbMembershipOptions> configure)
        {
            if (builder is null) 
                throw new ArgumentNullException(nameof(builder));
            if (configure is null) 
                throw new ArgumentNullException(nameof(configure));

            return builder.ConfigureServices(services =>
            {
                services.AddOptions<RavenDbMembershipOptions>()
                        .Configure(configure)
                        .Validate(o => o.Urls is { Length: > 0 }, "RavenDB Urls must be provided.")
                        .Validate(o => !string.IsNullOrWhiteSpace(o.DatabaseName), "RavenDB DatabaseName must be provided.")
                        .Validate(o => !string.IsNullOrWhiteSpace(o.ClusterId), "ClusterId must be provided.");

                RegisterGatewayListProvider(services);
            });
        }

        /// <summary>
        /// Configure Orleans client clustering to discover gateways via RavenDB using an IConfiguration section.
        /// </summary>
        public static IClientBuilder UseRavenDbClustering(
            this IClientBuilder builder,
            IConfiguration configurationSection)
        {
            if (builder is null) 
                throw new ArgumentNullException(nameof(builder));
            if (configurationSection is null) 
                throw new ArgumentNullException(nameof(configurationSection));

            return builder.ConfigureServices(services =>
            {
                services.AddOptions<RavenDbMembershipOptions>()
                        .Bind(configurationSection)
                        .Validate(o => o.Urls is { Length: > 0 }, "RavenDB Urls must be provided.")
                        .Validate(o => !string.IsNullOrWhiteSpace(o.DatabaseName), "RavenDB DatabaseName must be provided.")
                        .Validate(o => !string.IsNullOrWhiteSpace(o.ClusterId), "ClusterId must be provided.");

                RegisterGatewayListProvider(services);
            });
        }

        private static void RegisterGatewayListProvider(IServiceCollection services)
        {
            services.AddSingleton<IGatewayListProvider>(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<RavenDbMembershipOptions>>();
                var logger = sp.GetRequiredService<ILogger<RavenDbGatewayListProvider>>();
                return new RavenDbGatewayListProvider(opts, logger);
            });
        }
    }
}
