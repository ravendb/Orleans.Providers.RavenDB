namespace Orleans.Providers.RavenDb.Configuration
{
    public sealed class RavenDbReminderOptionsValidator : IConfigurationValidator
    {
        private readonly RavenDbReminderOptions _options;
        private readonly string _name;

        public RavenDbReminderOptionsValidator(RavenDbReminderOptions options)
        {
            _options = options;
            //_name = name;
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
