using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Messaging;
using Orleans.Providers.RavenDB.Configuration;
using Raven.Embedded;
using TestExtensions;
using UnitTests;
using UnitTests.MembershipTests;
using Xunit;

[TestCategory("Membership")]
public class RavenDbMembershipTableTests : MembershipTableTestsBase
{

    private const string MembershipTableTestsDatabase = "OrleansMembershipTableTests";


    public RavenDbMembershipTableTests(ConnectionStringFixture fixture, TestEnvironmentFixture clusterFixture)
        : base(fixture, clusterFixture, new LoggerFilterOptions())
    {
    }

    protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
    {
        var serverUrl = GetConnectionString().GetAwaiter().GetResult();

        var options = new RavenDbMembershipOptions
        {
            Urls = new[] { serverUrl },
            DatabaseName = MembershipTableTestsDatabase,
            ClusterId = clusterId,
            WaitForIndexesAfterSaveChanges = true
        };

        return new RavenDbGatewayListProvider(Options.Create(options), logger);
    }

    protected override IMembershipTable CreateMembershipTable(ILogger logger)
    {
        var serverUrl = GetConnectionString().GetAwaiter().GetResult();

        var options = new RavenDbMembershipOptions
        {
            Urls = new[] { serverUrl },
            DatabaseName = MembershipTableTestsDatabase,
            ClusterId = clusterId,
            WaitForIndexesAfterSaveChanges = true
        };

        return new RavenDbMembershipTable(options, NullLogger<RavenDbMembershipTable>.Instance);
    }

    protected override async Task<string> GetConnectionString()
    {
        return (await _lazyServerUrl.Value).AbsoluteUri;

    }

    private static readonly Lazy<Task<Uri>> _lazyServerUrl = new(() =>
    {
        EmbeddedServer.Instance.StartServer();
        return EmbeddedServer.Instance.GetServerUriAsync();
    });

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
}