﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Storage;

namespace Orleans.Providers.RavenDb.StorageProviders;

/// <summary>
/// Extension methods for configuring RavenDB grain storage in Orleans.
/// </summary>
public static class RavenDbGrainStorageExtensions
{
    public static ISiloBuilder AddRavenDbGrainStorage(this ISiloBuilder builder, string name, Action<RavenDbOptions> configureOptions)
    {
        return builder.ConfigureServices(services => services.AddRavenDbGrainStorage(name, configureOptions));
    }

    public static IServiceCollection AddRavenDbGrainStorage(this IServiceCollection services, string name, Action<RavenDbOptions> configureOptions)
    {
        return services.AddRavenDbGrainStorage(name, ob => ob.Configure(configureOptions));
    }

    public static ISiloBuilder AddRavenDbGrainStorageAsDefault(this ISiloBuilder builder, Action<RavenDbOptions> configureOptions)
    {
        return builder.AddRavenDbGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    public static IServiceCollection AddRavenDbGrainStorage(this IServiceCollection services, string name, Action<OptionsBuilder<RavenDbOptions>> configureOptions)
    {
        configureOptions?.Invoke(services.AddOptions<RavenDbOptions>(name));
        services.AddTransient<IConfigurationValidator>(sp => new RavenDbGrainStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<RavenDbOptions>>().Get(name), name));
        services.ConfigureNamedOptionForLogging<RavenDbOptions>(name);
        services.AddTransient<IPostConfigureOptions<RavenDbOptions>, RavenDbGrainStorageConfigurator>();

        services.AddKeyedSingleton(name, (sp, key) => RavenDbGrainStorageFactory.Create(sp, key as string));
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
            services.AddSingleton(sp => sp.GetKeyedService<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));

        services.AddSingleton(s => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredKeyedService<IGrainStorage>(name));

        return services;
    }
}