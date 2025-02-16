using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Messaging;
using Orleans.Providers.RavenDB.Configuration;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;
using TestExtensions;
using UnitTests;
using UnitTests.Infrastructure;
using UnitTests.MembershipTests;
using Xunit;

[TestCategory("Membership")]
public class RavenDbMembershipTableTests : MembershipTableTestsBase/*, IAsyncLifetime*/, IClassFixture<RavenDbFixture>
{
    private static string MembershipTableTestsDatabase = "OrleansMembershipTableTests-" + Guid.NewGuid();
    private readonly RavenDbFixture _fixture;
    private RavenDbMembershipTable _membershipTable;

    public RavenDbMembershipTableTests(ConnectionStringFixture fixture, RavenDbFixture ravenDbFixture, TestEnvironmentFixture clusterFixture)
        : base(fixture, clusterFixture, new LoggerFilterOptions())
    {
        _fixture = ravenDbFixture;
    }

    //public Task InitializeAsync()
    //{
    //    //MembershipOptions.DatabaseName = _fixture.TestDatabaseName;
    //    //MembershipOptions.Urls = [RavenDbFixture.ServerUrl.AbsoluteUri];

    //    return Task.CompletedTask;
    //}

    protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
    {
        //var serverUrl = GetConnectionString().GetAwaiter().GetResult();

        //var options = new RavenDbMembershipOptions
        //{
        //    Urls = new[] { serverUrl },
        //    DatabaseName = MembershipTableTestsDatabase,
        //    ClusterId = clusterId,
        //    WaitForIndexesAfterSaveChanges = true
        //};

        return new RavenDbGatewayListProvider(Options.Create(MembershipOptions), logger);
    }

    protected override IMembershipTable CreateMembershipTable(ILogger logger)
    {
        //var serverUrl = GetConnectionString().GetAwaiter().GetResult();

        MembershipOptions = new RavenDbMembershipOptions
        {
            Urls = new[] { RavenDbFixture.ServerUrl.AbsoluteUri },
            DatabaseName = MembershipTableTestsDatabase,
            ClusterId = clusterId,
            WaitForIndexesAfterSaveChanges = true
        };

        _membershipTable = new RavenDbMembershipTable(MembershipOptions, NullLogger<RavenDbMembershipTable>.Instance);
        return _membershipTable;
    }

    public RavenDbMembershipOptions MembershipOptions { get; set; }

    protected override async Task<string> GetConnectionString()
    {
        return RavenDbFixture.ServerUrl.AbsoluteUri;
    }

    //private static readonly Lazy<Task<Uri>> _lazyServerUrl = new(() =>
    //{
    //    EmbeddedServer.Instance.StartServer();
    //    return EmbeddedServer.Instance.GetServerUriAsync();
    //});

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
        await MembershipTable_InsertRow();
    }

    [Fact]
    public async Task Test_ReadRow_Insert_Read()
    {
        await MembershipTable_ReadRow_Insert_Read();
    }

    [Fact]
    public async Task Test_ReadAll_Insert_ReadAll()
    {
        await MembershipTable_ReadAll_Insert_ReadAll();
    }

    [Fact]
    public async Task Test_UpdateRow()
    {
        await MembershipTable_UpdateRow();
    }

    [Fact]
    public async Task Test_UpdateRowInParallel()
    {
        await MembershipTable_UpdateRowInParallel();
    }

    [Fact]
    public async Task Test_UpdateIAmAlive()
    {
        await MembershipTable_UpdateIAmAlive();
    }



    //public async Task DisposeAsync()
    //{
    //    //await _membershipTable.DeleteMembershipTableEntries(clusterId);
    //    _membershipTable = null;
    //    Dispose();
    //    //await _fixture.DocumentStore.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(MembershipTableTestsDatabase, hardDelete: true));
    //}
}