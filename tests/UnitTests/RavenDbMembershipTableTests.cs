using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        return new RavenDbGatewayListProvider(Options.Create(MembershipOptions), NullLogger<RavenDbGatewayListProvider>.Instance);
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

    [Fact]
    public void Test_DeleteMembershipTableEntries_AwaitsQueryInsteadOfBlocking()
    {
        var method = typeof(RavenDbMembershipTable).GetMethod(nameof(IMembershipTable.DeleteMembershipTableEntries));
        Assert.NotNull(method);

        var stateMachine = method!.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        Assert.NotNull(stateMachine);

        // Blocking on Task.Result leaves no awaiter for the query in the generated state machine,
        // so its presence is a direct, deterministic signal that the query is awaited. Asserting
        // this by reflection rather than by timing avoids a test that depends on a deadlock
        // actually occurring, which is scheduler-dependent and would be flaky.
        var awaitsQuery = stateMachine!
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(field => field.FieldType == typeof(TaskAwaiter<List<MembershipEntryDocument>>));

        Assert.True(awaitsQuery,
            "DeleteMembershipTableEntries must await the membership query. Blocking on Task.Result "
            + "occupies an Orleans scheduler thread for the duration of a network round trip and can deadlock the silo.");
    }
}