using Orleans;
using Orleans.Runtime;

namespace UnitTests.Grains;

public interface IMembershipTestGrain : IGrainWithStringKey
{
    Task InitializeMembershipTable(bool tryInitTable);

    Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion);

    Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion);

    Task<MembershipTableData> ReadRow(SiloAddress siloAddress);

    Task<MembershipTableData> ReadAll();

    Task CleanupDefunctSiloEntries(DateTimeOffset beforeDate);

    Task DeleteMembershipTableEntries(string clusterId);

}
