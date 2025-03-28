﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Storage;

namespace Orleans.Providers.RavenDb.StorageProviders
{
    /// <summary>
    /// Factory class for creating instances of <see cref="RavenDbGrainStorage"/>.
    /// </summary>
    public static class RavenDbGrainStorageFactory
    {
        public static IGrainStorage Create(IServiceProvider services, string name)
        {
            var optionsMonitor = services.GetRequiredService<IOptionsMonitor<RavenDbGrainStorageOptions>>();

            var options = optionsMonitor.Get(name);

            return ActivatorUtilities.CreateInstance<RavenDbGrainStorage>(services, options);
        }
    }
}
