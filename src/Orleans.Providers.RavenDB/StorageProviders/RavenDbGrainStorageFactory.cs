using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Storage;

namespace Orleans.Providers.RavenDb.StorageProviders
{
    public static class RavenDbGrainStorageFactory
    {
        /// <summary>
        /// Factory class for creating instances of <see cref="RavenDbGrainStorage"/>.
        /// </summary>
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            var optionsMonitor = services.GetRequiredService<IOptionsMonitor<RavenDbOptions>>();
            
            return ActivatorUtilities.CreateInstance<RavenDbGrainStorage>(services, optionsMonitor.Get(name));
        }
    }
}
