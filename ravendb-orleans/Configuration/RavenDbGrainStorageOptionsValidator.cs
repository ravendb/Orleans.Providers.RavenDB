using Orleans.Runtime;

namespace Orleans.Providers.RavenDB.Configuration
{
    public sealed class RavenDbGrainStorageOptionsValidator : IConfigurationValidator
    {
        private readonly RavenDbOptions _options;
        private readonly string _name;

        public RavenDbGrainStorageOptionsValidator(RavenDbOptions options, string name)
        {
            _options = options;
            _name = name;
        }

        public void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(_options.DatabaseName))
                throw new OrleansConfigurationException(nameof(_options.DatabaseName));
            if (_options.Urls == null || _options.Urls.Length == 0)
                throw new OrleansConfigurationException(nameof(_options.Urls));

        }
    }
}
