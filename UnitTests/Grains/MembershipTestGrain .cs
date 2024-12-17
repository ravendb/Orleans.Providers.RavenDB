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

    public Task<bool> InitializeMembershipTable(bool tryInitTable)
    {
        try
        {
            _membershipTable.InitializeMembershipTable(tryInitTable);
        }
        catch
        {
            return Task.FromResult(false);
        }

        // If no exception, initialization is successful
        return Task.FromResult(true);
    }

    public Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        return _membershipTable.InsertRow(entry, tableVersion);
    }

    public Task<MembershipTableData> ReadRow(SiloAddress siloAddress)
    {
        return _membershipTable.ReadRow(siloAddress);
    }

    public Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate)
    {
        return _membershipTable.CleanupDefunctSiloEntries(beforeDate);
    }
}
