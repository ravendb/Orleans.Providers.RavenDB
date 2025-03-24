using Microsoft.Extensions.DependencyInjection;
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
    /// <summary>
    /// Adds RavenDB grain storage to the silo with a named provider.
    /// </summary>
    /// <param name="builder">The Orleans silo builder.</param>
    /// <param name="name">The name of the storage provider.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="RavenDbGrainStorageOptions"/>.</param>
    /// <returns>The updated <see cref="ISiloBuilder"/>.</returns>
    public static ISiloBuilder AddRavenDbGrainStorage(this ISiloBuilder builder, string name, Action<RavenDbGrainStorageOptions> configureOptions)
    {
        return builder.ConfigureServices(services => services.AddRavenDbGrainStorage(name, configureOptions));
    }

    /// <summary>
    /// Adds RavenDB grain storage to the service collection with a named provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the storage provider.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="RavenDbGrainStorageOptions"/>.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRavenDbGrainStorage(this IServiceCollection services, string name, Action<RavenDbGrainStorageOptions> configureOptions)
    {
        return services.AddRavenDbGrainStorage(name, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    /// Adds RavenDB grain storage to the service collection as the default storage provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="RavenDbGrainStorageOptions"/>.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRavenDbGrainStorageAsDefault(this IServiceCollection services, Action<RavenDbGrainStorageOptions> configureOptions)
    {
        return services.AddRavenDbGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, ob => ob.Configure(configureOptions));
    }

    /// <summary>
    /// Adds RavenDB grain storage to the silo as the default storage provider.
    /// </summary>
    /// <param name="builder">The Orleans silo builder.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="RavenDbGrainStorageOptions"/>.</param>
    /// <returns>The updated <see cref="ISiloBuilder"/>.</returns>
    public static ISiloBuilder AddRavenDbGrainStorageAsDefault(this ISiloBuilder builder, Action<RavenDbGrainStorageOptions> configureOptions)
    {
        return builder.AddRavenDbGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    /// <summary>
    /// Adds RavenDB grain storage to the service collection, allowing advanced configuration using <see cref="OptionsBuilder{TOptions}"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The name of the storage provider.</param>
    /// <param name="configureOptions">
    /// A delegate to configure the <see cref="OptionsBuilder{RavenDbGrainStorageOptions}"/>, 
    /// allowing validation, binding, or other advanced setup.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRavenDbGrainStorage(this IServiceCollection services, string name, Action<OptionsBuilder<RavenDbGrainStorageOptions>> configureOptions)
    {
        configureOptions?.Invoke(services.AddOptions<RavenDbGrainStorageOptions>(name));
        services.AddTransient<IConfigurationValidator>(sp => new RavenDbGrainStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<RavenDbGrainStorageOptions>>().Get(name), name));
        services.ConfigureNamedOptionForLogging<RavenDbGrainStorageOptions>(name);
        services.AddTransient<IPostConfigureOptions<RavenDbGrainStorageOptions>, RavenDbGrainStorageConfigurator>();

        services.AddKeyedSingleton(name, (sp, key) => RavenDbGrainStorageFactory.Create(sp, key as string));
        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
            services.AddSingleton(sp => sp.GetKeyedService<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));

        services.AddSingleton(s => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredKeyedService<IGrainStorage>(name));

        return services;
    }
}