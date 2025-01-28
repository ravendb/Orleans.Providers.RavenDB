using Orleans;
using Orleans.Runtime;

namespace UnitTests.Grains;

public class MembershipTestGrain : Grain, IMembershipTestGrain
{
    private readonly IMembershipTable _membershipTable;

    public MembershipTestGrain(IMembershipTable membershipTable)
    {
        _membershipTable = membershipTable;
    }

    public Task InitializeMembershipTable(bool tryInitTable)
    {
        _membershipTable.InitializeMembershipTable(tryInitTable);
        return Task.CompletedTask;
    }

    public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        return _membershipTable.InsertRow(entry, tableVersion);
    }

    public Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        return _membershipTable.UpdateRow(entry, etag, tableVersion);
    }

    public Task<MembershipTableData> ReadRow(SiloAddress siloAddress)
    {
        return _membershipTable.ReadRow(siloAddress);
    }

    public Task<MembershipTableData> ReadAll()
    {
        return _membershipTable.ReadAll();
    }

    public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
    {
        return _membershipTable.CleanupDefunctSiloEntries(beforeDate);
    }

    public Task DeleteMembershipTableEntries(string clusterId)
    {
        return _membershipTable.DeleteMembershipTableEntries(clusterId);
    }
}
