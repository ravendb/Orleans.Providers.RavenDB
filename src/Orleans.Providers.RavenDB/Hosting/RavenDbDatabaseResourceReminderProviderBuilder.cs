using Microsoft.Extensions.Configuration;
using Orleans.Providers.RavenDb.Hosting;
using Orleans.Providers.RavenDb.Reminders;

[assembly: RegisterProvider("RavenDBDatabase", "Reminders", "Silo", typeof(RavenDbDatabaseResourceReminderProviderBuilder))]

namespace Orleans.Providers.RavenDb.Hosting;

internal sealed class RavenDbDatabaseResourceReminderProviderBuilder : IProviderBuilder<ISiloBuilder>
{
    public void Configure(ISiloBuilder builder, string? name, IConfigurationSection section)
    {
        RavenDbDatabaseResourceClusteringProviderBuilder.ConfigureSectionFromServiceKey(builder.Configuration, section);
        builder.AddRavenDbReminderTable(section.Bind);
    }
}