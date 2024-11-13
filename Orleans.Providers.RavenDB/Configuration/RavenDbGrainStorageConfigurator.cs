using Microsoft.Extensions.Options;

namespace Orleans.Providers.RavenDB.Configuration
{
    internal class RavenDbGrainStorageConfigurator : IPostConfigureOptions<RavenDbGrainStorageOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public RavenDbGrainStorageConfigurator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public void PostConfigure(string name, RavenDbGrainStorageOptions options)
        {
        }
    }
}
