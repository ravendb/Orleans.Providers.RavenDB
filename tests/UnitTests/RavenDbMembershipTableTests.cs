using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Messaging;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Membership;
using TestExtensions;
using UnitTests;
using UnitTests.Infrastructure;
using UnitTests.MembershipTests;
using Xunit;

[TestCategory("Membership")]
public class RavenDbMembershipTableTests : MembershipTableTestsBase, IClassFixture<RavenDbFixture>
{
    private static string MembershipTableTestsDatabase = "OrleansMembershipTableTests-" + Guid.NewGuid();
    private RavenDbMembershipTable _membershipTable;

    public RavenDbMembershipTableTests(ConnectionStringFixture fixture, RavenDbFixture ravenDbFixture, TestEnvironmentFixture clusterFixture)
        : base(fixture, clusterFixture, new LoggerFilterOptions())
    {
    }

    protected override IGatewayListProvider CreateGatewayListProvider(ILogger logger)
    {
        return new RavenDbGatewayListProvider(Options.Create(MembershipOptions), logger);
    }

    protected override IMembershipTable CreateMembershipTable(ILogger logger)
    {
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

    protected override Task<string> GetConnectionString()
    {
        return Task.FromResult(RavenDbFixture.ServerUrl.AbsoluteUri);
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