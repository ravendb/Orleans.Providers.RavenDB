using Microsoft.Extensions.Configuration;
using Orleans.Providers.RavenDb.Hosting;
using Orleans.Providers.RavenDb.Reminders;

[assembly: RegisterProvider("RavenDbReminders", "Reminders", "Silo", typeof(RavenDbReminderProviderBuilder))]

namespace Orleans.Providers.RavenDb.Hosting;

internal sealed class RavenDbReminderProviderBuilder : IProviderBuilder<ISiloBuilder>
{
    public void Configure(ISiloBuilder builder, string? name, IConfigurationSection section)
    {
        builder.AddRavenDbReminderTable(section.Bind);
    }
}