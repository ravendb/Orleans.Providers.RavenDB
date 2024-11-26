using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Storage;

namespace Orleans.Providers.RavenDB.StorageProviders
{
    public static class RavenDbGrainStorageFactory
    {
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            var optionsMonitor = services.GetRequiredService<IOptionsMonitor<RavenDbOptions>>();
            
            return ActivatorUtilities.CreateInstance<RavenDbGrainStorage>(services, optionsMonitor.Get(name));
        }
    }
}
