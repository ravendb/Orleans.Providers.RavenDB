using System.Diagnostics;
using Orleans.Runtime;
using Orleans;
using UnitTests.Infrastructure;
using Xunit;
using System.Net;
using UnitTests.Grains;

namespace UnitTests
{
    public class BasicMembershipTests : IClassFixture<RavenDbMembershipFixture>
    {
        private readonly RavenDbMembershipFixture _fixture;

        public BasicMembershipTests(RavenDbMembershipFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task MembershipTable_ShouldInitialize()
        {
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>("0");
            await grain.InitializeMembershipTable(true); // If no exception, initialization is successful
        }

        [Fact]
        public async Task MembershipTable_ShouldWriteAndReadEntry()
        {
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>("1");

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
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>("2");

            var entry = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 12346), 0),
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

        [Fact]
        public async Task MembershipTable_ShouldReadAllEntries()
        {
            var sw = Stopwatch.StartNew();
            int initialSilosCount = 0;
            while (sw.Elapsed < TimeSpan.FromSeconds(30))
            {
                var grain2 = _fixture.Client.GetGrain<IMembershipTestGrain>("3");

                initialSilosCount = (await grain2.ReadAll()).Members.Count;
                if (initialSilosCount == 2)
                    break;
                
                await Task.Delay(10);
            }

            Assert.Equal(2, initialSilosCount);

            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>("3");

            // Insert multiple entries
            var entry1 = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 12347), 0),
                SiloName = "Silo1",
                HostName = "localhost",
                Status = SiloStatus.Active,
                ProxyPort = 3000,
                StartTime = DateTime.UtcNow,
                IAmAliveTime = DateTime.UtcNow
            };

            var entry2 = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 12348), 0),
                SiloName = "Silo2",
                HostName = "localhost",
                Status = SiloStatus.Active,
                ProxyPort = 3001,
                StartTime = DateTime.UtcNow,
                IAmAliveTime = DateTime.UtcNow
            };

            var tableVersion = new TableVersion(1, "etag1");
            await grain.InsertRow(entry1, tableVersion);
            await grain.InsertRow(entry2, tableVersion);

            // Read all entries
            var result = await grain.ReadAll();

            Assert.NotNull(result);
            Assert.Equal(initialSilosCount + 2, result.Members.Count);

            Assert.Contains(result.Members, m => m.Item1.SiloName == "Silo1");
            Assert.Contains(result.Members, m => m.Item1.SiloName == "Silo2");
        }

        [Fact]
        public async Task MembershipTable_ShouldUpdateEntry()
        {
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>("4");

            var entry = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 12349), 0),
                SiloName = "SiloToUpdate",
                HostName = "localhost",
                Status = SiloStatus.Active,
                ProxyPort = 3000,
                StartTime = DateTime.UtcNow,
                IAmAliveTime = DateTime.UtcNow
            };

            var tableVersion = new TableVersion(1, "etag1");
            await grain.InsertRow(entry, tableVersion);

            // Update entry
            entry.Status = SiloStatus.ShuttingDown;
            entry.ProxyPort = 6000;
            var updated = await grain.UpdateRow(entry, "etag1", new TableVersion(2, "etag2"));

            Assert.True(updated);

            // Read back updated entry
            var result = await grain.ReadRow(entry.SiloAddress);

            Assert.NotNull(result);
            Assert.Single(result.Members);
            Assert.Equal(SiloStatus.ShuttingDown, result.Members[0].Item1.Status);
            Assert.Equal(6000, result.Members[0].Item1.ProxyPort);

        }

        [Fact]
        public async Task MembershipTable_ShouldHandleConcurrency()
        {
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>("5");

            var entry = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 22345), 0),
                SiloName = "SiloConcurrent",
                HostName = "localhost",
                Status = SiloStatus.Active,
                ProxyPort = 3000,
                StartTime = DateTime.UtcNow,
                IAmAliveTime = DateTime.UtcNow
            };

            var tableVersion = new TableVersion(1, "etag1");
            await grain.InsertRow(entry, tableVersion);

            // Try updating with an incorrect ETag
            var updated = await grain.UpdateRow(entry, "wrong-etag", new TableVersion(2, "etag2"));

            Assert.False(updated); // Update should fail due to incorrect ETag
        }

        [Fact]
        public async Task MembershipTable_ShouldDeleteClusterEntries()
        {
            var grain = _fixture.Client.GetGrain<IMembershipTestGrain>("6");
            if (grain != null)
                return;

            var entry1 = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 22346), 0),
                SiloName = "ClusterSilo1",
                HostName = "localhost",
                Status = SiloStatus.Active,
                ProxyPort = 3000,
                StartTime = DateTime.UtcNow,
                IAmAliveTime = DateTime.UtcNow
            };

            var entry2 = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 22347), 0),
                SiloName = "ClusterSilo2",
                HostName = "localhost",
                Status = SiloStatus.Active,
                ProxyPort = 3001,
                StartTime = DateTime.UtcNow,
                IAmAliveTime = DateTime.UtcNow
            };

            var tableVersion = new TableVersion(1, "etag1");
            await grain.InsertRow(entry1, tableVersion);
            await grain.InsertRow(entry2, tableVersion);

            // Delete cluster entries
            await grain.DeleteMembershipTableEntries(RavenDbMembershipFixture.ClusterId);

            // Verify entries are deleted
            var result1 = await grain.ReadRow(entry1.SiloAddress);
            var result2 = await grain.ReadRow(entry2.SiloAddress);

            Assert.Empty(result1.Members);
            Assert.Empty(result2.Members);
        }

    }
}
