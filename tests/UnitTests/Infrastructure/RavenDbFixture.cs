using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;

namespace UnitTests.Infrastructure;


public class RavenDbFixture : BaseTestClusterFixture
{
    public IDocumentStore DocumentStore;
    public string TestDatabaseName {get; private set; }

    static RavenDbFixture()
    {
        EmbeddedServer.Instance.StartServer();
        ServerUrl = EmbeddedServer.Instance.GetServerUriAsync().Result;
    }

    public static Uri ServerUrl { get; }

    public override Task InitializeAsync()
    {
        TestDatabaseName = Guid.NewGuid().ToString();
        DocumentStore = EmbeddedServer.Instance.GetDocumentStore(TestDatabaseName);
        return base.InitializeAsync();
    }

    public override Task DisposeAsync()
    {
        try
        {
            DocumentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(TestDatabaseName, hardDelete: true));
            DocumentStore.Dispose();
            return base.DisposeAsync();
        }
        catch
        {
            return Task.CompletedTask;
        }
    }
}
