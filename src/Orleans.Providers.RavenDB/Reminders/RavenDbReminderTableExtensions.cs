using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.RavenDb.Configuration;

namespace Orleans.Providers.RavenDb.Reminders
{
    public static class RavenDbMembershipTableExtensions
    {
        public static ISiloBuilder AddRavenDbReminderTable(this ISiloBuilder builder, Action<RavenDbReminderOptions> configureOptions)
        {
            return builder.ConfigureServices(services =>
            {
                // Explicitly register RavenDbReminderOptions as a singleton
                var options = new RavenDbReminderOptions();
                configureOptions(options);
                services.AddSingleton(options);

                // Now register the reminder services
                services.AddReminders();
                services.AddSingleton<IReminderTable, RavenDbReminderTable>();
                services.AddSingleton<IConfigurationValidator, RavenDbReminderOptionsValidator>();
            });
        }
    }


}
