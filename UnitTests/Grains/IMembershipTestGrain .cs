using Orleans;
using Orleans.Runtime;

namespace UnitTests.Grains;

public interface IMembershipTestGrain : IGrainWithIntegerKey
{
    Task<bool> InitializeMembershipTable(bool tryInitTable);
    Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion);
    Task<MembershipTableData> ReadRow(SiloAddress siloAddress);
    Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate);
}