using Microsoft.Extensions.Configuration;
using Orleans.Providers.RavenDb.Hosting;
using Orleans.Providers.RavenDb.Membership;
using System.Data.Common;

[assembly: RegisterProvider("RavenDBDatabase", "Clustering", "Silo", typeof(RavenDbDatabaseResourceClusteringProviderBuilder))]


namespace Orleans.Providers.RavenDb.Hosting
{
    internal sealed class RavenDbDatabaseResourceClusteringProviderBuilder : IProviderBuilder<ISiloBuilder>
    {
        public void Configure(ISiloBuilder builder, string? name, IConfigurationSection section)
        {
            ConfigureSectionFromServiceKey(builder.Configuration, section);
            builder.UseRavenDbMembershipTable(section.Bind);
        }

        public static void ConfigureSectionFromServiceKey(IConfiguration configuration, IConfigurationSection section)
        {
            var serviceKey = section["ServiceKey"];
            if (serviceKey == null)
                throw new InvalidOperationException("Missing 'ServiceKey' environment variable.");

            var ravenCs = configuration.GetConnectionString(serviceKey);
            if (ravenCs == null)
                throw new InvalidOperationException("RavenDB connection string is missing.");

            var csBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = ravenCs
            };

            if (!csBuilder.TryGetValue("Url", out var url))
                throw new InvalidOperationException("RavenDB connection string is missing 'Url'.");

            if (!csBuilder.TryGetValue("Database", out var databaseName))
                throw new InvalidOperationException("RavenDB connection string is missing 'Database'.");

            section["Urls:0"] = url.ToString();
            section["DatabaseName"] = databaseName.ToString();

            foreach (string key in csBuilder.Keys)
            {
                if (key.ToLower() is "url" or "database")
                    continue;

                section[key] = csBuilder[key].ToString();
            }

            var clusterIdOption = section["ClusterId"];
            if (clusterIdOption == null)
            {
                // look for clusterId in global orleans configuration
                var clusterId = configuration.GetSection("Orleans:ClusterId");
                if (clusterId.Value != null)
                {
                    section["ClusterId"] = clusterId.Value;
                }
            }
        }
    }
}