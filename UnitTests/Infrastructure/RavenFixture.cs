using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;

namespace UnitTests.Infrastructure;

public class RavenFixture : Xunit.IAsyncLifetime
{
    public IHost Host { get; }

    public IClusterClient Client { get; }

    protected readonly string ServerUrl;

    private Action? _onDispose;

    protected RavenFixture()
    {
        EmbeddedServer.Instance.StartServer();
        ServerUrl = EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult().AbsoluteUri;

        Host = new HostBuilder()
            .UseOrleans((ctx, builder) =>
            {
                builder.UseLocalhostClustering();
                PreBuild(builder);

            })
            .Build();

        Client = Host.Services.GetRequiredService<IClusterClient>();
    }

    protected virtual ISiloBuilder PreBuild(ISiloBuilder builder) { return builder; }

    protected IDocumentStore GetDocumentStore(string database)
    {
        var store = EmbeddedServer.Instance.GetDocumentStore(database);

        _onDispose += () =>
        {
            store.Maintenance.Server.Send(new DeleteDatabasesOperation(database, hardDelete: true));
            store.Dispose();
        };

        return store;
    }


    public Task InitializeAsync()
    {
        //await base.InitializeAsync();
        return Host.StartAsync();
    }

    public Task DisposeAsync()
    {
        //await base.DisposeAsync();
        try
        {
            _onDispose?.Invoke();

            EmbeddedServer.Instance.Dispose();

            return Host.StopAsync();
        }
        catch
        {
            return Task.CompletedTask;
        }
    }
}