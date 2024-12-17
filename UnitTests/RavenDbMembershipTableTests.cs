using Orleans.Runtime;
using Orleans;
using UnitTests.Infrastructure;
using Xunit;
using System.Net;
using UnitTests.Grains;

namespace UnitTests
{
    public class RavenDbMembershipTableTests : IClassFixture<RavenDbMembershipFixture>
    {
        private readonly RavenDbMembershipFixture _fixture;

        public RavenDbMembershipTableTests(RavenDbMembershipFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task MembershipTable_ShouldInitialize()
        {
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>(0);
            var initialized = await grain.InitializeMembershipTable(true);

            Assert.True(initialized); // If no exception, initialization is successful
        }

        [Fact]
        public async Task MembershipTable_ShouldWriteAndReadEntry()
        {
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>(0);

            var entry = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 12345), 0),
                SiloName = "TestSilo",
                HostName = "localhost",
                Status = SiloStatus.Active,
                ProxyPort = 3000,
                StartTime = DateTime.UtcNow,
                IAmAliveTime = DateTime.UtcNow
            };

            var tableVersion = new TableVersion(1, "etag1");
            var inserted = await grain.InsertRow(entry, tableVersion);

            Assert.True(inserted);

            var result = await grain.ReadRow(entry.SiloAddress);

            Assert.NotNull(result);
            Assert.Single(result.Members);
            Assert.Equal(entry.SiloName, result.Members[0].Item1.SiloName);
        }

        [Fact]
        public async Task MembershipTable_ShouldCleanupDefunctEntries()
        {
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>(0);

            var entry = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 12345), 0),
                SiloName = "DefunctSilo",
                HostName = "localhost",
                Status = SiloStatus.Dead,
                ProxyPort = 3000,
                StartTime = DateTime.UtcNow.AddDays(-2),
                IAmAliveTime = DateTime.UtcNow.AddDays(-1)
            };

            var tableVersion = new TableVersion(1, "etag1");
            await grain.InsertRow(entry, tableVersion);

            await grain.CleanupDefunctSiloEntries(DateTimeOffset.UtcNow.AddDays(-1));

            var result = await grain.ReadRow(entry.SiloAddress);
            Assert.Empty(result.Members); // Entry should be cleaned up
        }
    }


}
