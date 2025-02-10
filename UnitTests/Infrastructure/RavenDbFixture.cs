using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;

namespace UnitTests.Infrastructure;


public class RavenDbFixture : BaseTestClusterFixture
{
    public IDocumentStore DocumentStore;

    protected virtual string TestDatabaseName {get; set; }


    public override Task InitializeAsync()
    {
        EmbeddedServer.Instance.StartServer();

        DocumentStore = EmbeddedServer.Instance.GetDocumentStore(TestDatabaseName);

        return base.InitializeAsync();
    }

    public override Task DisposeAsync()
    {
        try
        {
            DocumentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(TestDatabaseName, hardDelete: true));
            DocumentStore.Dispose();

            EmbeddedServer.Instance.Dispose();

            return base.DisposeAsync();
        }
        catch
        {
            return Task.CompletedTask;
        }
    }
}
