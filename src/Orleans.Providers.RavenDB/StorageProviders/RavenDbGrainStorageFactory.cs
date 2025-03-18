using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Serialization;
using Orleans.Storage;
using Raven.Client.Documents.Conventions;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Orleans.Providers.RavenDb.StorageProviders
{
    public static class RavenDbGrainStorageFactory
    {
        /// <summary>
        /// Factory class for creating instances of <see cref="RavenDbGrainStorage"/>.
        /// </summary>
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            var optionsMonitor = services.GetRequiredService<IOptionsMonitor<RavenDbGrainStorageOptions>>();

            var options = optionsMonitor.Get(name);

            return ActivatorUtilities.CreateInstance<RavenDbGrainStorage>(services, options);
        }
    }
}
