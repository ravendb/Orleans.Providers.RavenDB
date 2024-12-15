using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers.RavenDB.Configuration;
using Raven.Client.Documents;

public static class RavenDbMembershipTableExtensions
{
    public static ISiloBuilder UseRavenDbMembershipTable(this ISiloBuilder builder, Action<RavenDbMembershipOptions> configureOptions)
    {
        return builder.ConfigureServices(services =>
        {
            services.Configure(configureOptions);
            services.AddSingleton<IMembershipTable, RavenDbMembershipTable>();
            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<RavenDbMembershipOptions>>().Value;
                return new DocumentStore
                {
                    Urls = options.Urls,
                    Database = options.DatabaseName,
                    Certificate = string.IsNullOrEmpty(options.CertificatePath)
                        ? null
                        : new X509Certificate2(options.CertificatePath)
                }.Initialize();
            });
        });
    }
}