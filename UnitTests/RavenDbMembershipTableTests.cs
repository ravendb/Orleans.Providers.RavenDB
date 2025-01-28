using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Messaging;
using Orleans.Providers.RavenDB.Configuration;
using Raven.Client.Documents;
using Raven.Embedded;
using TestExtensions;
using UnitTests;
using UnitTests.Infrastructure;
using UnitTests.MembershipTests;
using Xunit;

[TestCategory("Membership")]

public class RavenDbMembershipTableTests : MembershipTableTestsBase
{
    private string _serverUrl;

    public RavenDbMembershipTableTests(ConnectionStringFixture fixture, TestEnvironmentFixture clusterFixture)
        : base(fixture, clusterFixture, new LoggerFilterOptions())
    {
    }

    protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
    {
        var options = new RavenDbMembershipOptions
        {
            Urls = new[] { _serverUrl },
            DatabaseName = RavenDbMembershipFixture.TestDatabaseName,
            ClusterId = clusterId
        };

        return new RavenDbGatewayListProvider(Options.Create(options), logger);
    }

    protected override IMembershipTable CreateMembershipTable(ILogger logger)
    {
        //EmbeddedServer.Instance.StartServer();
        //_serverUrl = EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult().AbsoluteUri;

        var options = new RavenDbMembershipOptions
        {
            Urls = new[] { _serverUrl },
            DatabaseName = RavenDbMembershipFixture.TestDatabaseName,
            ClusterId = clusterId
        };

        return new RavenDbMembershipTable(options, NullLogger<RavenDbMembershipTable>.Instance);
    }

    protected override Task<string> GetConnectionString()
    {
        EmbeddedServer.Instance.StartServer();
        _serverUrl = EmbeddedServer.Instance.GetServerUriAsync().GetAwaiter().GetResult().AbsoluteUri;

        return Task.FromResult(_serverUrl);
    }

    public void Dispose2()
    {
        var documentStore = new DocumentStore
        {
            Urls = new[] { "http://127.0.0.1:8080" },
            Database = "OrleansMembershipTest"
        }.Initialize();

        documentStore.Maintenance.Server.Send(new Raven.Client.ServerWide.Operations.DeleteDatabasesOperation("OrleansMembershipTest", hardDelete: true));
        documentStore.Dispose();

        base.Dispose();
    }

    [Fact]
    public async Task Test_CleanupDefunctSiloEntries()
    {
        await MembershipTable_CleanupDefunctSiloEntries();
    }

    [Fact]
    public async Task Test_GetGateways()
    {
        await MembershipTable_GetGateways();
    }

    [Fact]
    public async Task Test_ReadAll_EmptyTable()
    {
        await MembershipTable_ReadAll_EmptyTable();
    }

    [Fact]
    public async Task Test_InsertRow()
    {
        await MembershipTable_InsertRow(true);
    }

    [Fact]
    public async Task Test_ReadRow_Insert_Read()
    {
        await MembershipTable_ReadRow_Insert_Read(true);
    }

    [Fact]
    public async Task Test_ReadAll_Insert_ReadAll()
    {
        await MembershipTable_ReadAll_Insert_ReadAll(true);
    }

    [Fact]
    public async Task Test_UpdateRow()
    {
        await MembershipTable_UpdateRow(true);
    }

    [Fact]
    public async Task Test_UpdateRowInParallel()
    {
        await MembershipTable_UpdateRowInParallel(true);
    }

    [Fact]
    public async Task Test_UpdateIAmAlive()
    {
        await MembershipTable_UpdateIAmAlive(true);
    }
}