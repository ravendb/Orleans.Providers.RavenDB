using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Providers.RavenDb.Configuration;

namespace Orleans.Providers.RavenDb.Reminders
{
    /// <summary>
    /// Extension methods for configuring RavenDB reminder table in Orleans.
    /// </summary>
    public static class RavenDbMembershipTableExtensions
    {
        /// <summary>
        /// Configures Orleans to use RavenDB as the reminder table storage.
        /// </summary>
        /// <param name="builder">The silo builder.</param>
        /// <param name="configureOptions">An action to configure the RavenDB reminder table options.</param>
        /// <returns>The silo builder with RavenDB reminder table configured.</returns>
        public static ISiloBuilder AddRavenDbReminderTable(this ISiloBuilder builder, Action<RavenDbReminderOptions> configureOptions)
        {
            return builder.ConfigureServices(services =>
            {
                services.AddOptions<RavenDbReminderOptions>()
                    .Configure(configureOptions);

                services.AddSingleton(sp => sp.GetRequiredService<IOptions<RavenDbReminderOptions>>().Value);

                services.AddReminders();
                services.AddSingleton<IReminderTable, RavenDbReminderTable>();
                services.AddSingleton<IConfigurationValidator, RavenDbReminderOptionsValidator>();
            });
        }
    }


}
