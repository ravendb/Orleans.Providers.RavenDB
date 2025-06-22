using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Membership;
using Raven.Client.Documents;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests
{
    public class RavenDbMembershipTableOptionsTests
    {
        [Fact]
        public async Task Should_Not_Create_Database_When_EnsureDatabaseExists_Is_False()
        {
            var membershipOptions = new RavenDbMembershipOptions
            {
                Urls = new[] { RavenDbFixture.ServerUrl.AbsoluteUri },
                DatabaseName = "ShouldNotExist",
                EnsureDatabaseExists = false
            };

            var membershipTable = new RavenDbMembershipTable(membershipOptions, NullLogger<RavenDbMembershipTable>.Instance);

            var exception = await Assert.ThrowsAsync<OrleansException> (async () =>
            {
                await membershipTable.InitializeMembershipTable(true);
            });
            Assert.Contains("Database 'ShouldNotExist' does not exist", exception.Message);
        }

        [Fact]
        public async Task CanStoreMembershipEntriesForMultipleServiceIdsInSameDatabase()
        {
            using var documentStore = new DocumentStore
            {
                Urls = new[] { RavenDbFixture.ServerUrl.AbsoluteUri },
                Database = Guid.NewGuid().ToString()
            };
            documentStore.Initialize();

            var options1 = new RavenDbMembershipOptions
            {
                Urls = new[] { RavenDbFixture.ServerUrl.AbsoluteUri },
                ServiceId = "Service-A",
                DatabaseName = documentStore.Database,
                ClusterId = "19804f4c-9af4-493e-b40a-d441bd10eacd"
            };

            var options2 = new RavenDbMembershipOptions
            {
                Urls = new[] { RavenDbFixture.ServerUrl.AbsoluteUri },
                ServiceId = "Service-B",
                DatabaseName = documentStore.Database,
                ClusterId = "19804f4c-9af4-493e-b40a-d441bd10eacd",
                EnsureDatabaseExists = false // use the same database without creating a new one
            };

            var membership1 = new RavenDbMembershipTable(options1, NullLogger<RavenDbMembershipTable>.Instance);
            var membership2 = new RavenDbMembershipTable(options2, NullLogger<RavenDbMembershipTable>.Instance);

            await membership1.InitializeMembershipTable(true);
            await membership2.InitializeMembershipTable(true);

            var siloEntry1 = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 1111), 0),
                HostName = "NodeA"
            };

            var siloEntry2 = new MembershipEntry
            {
                SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 2222), 0),
                HostName = "NodeB"
            };

            var tableVersion = new TableVersion(0, "init");

            await membership1.InsertRow(siloEntry1, tableVersion);
            await membership2.InsertRow(siloEntry2, tableVersion);

            using (var session = documentStore.OpenAsyncSession())
            {
                var versionDocs = await session.Query<TableVersionDocument>().ToListAsync();
                Assert.Equal(2, versionDocs.Count);

                var membershipDocs = await session.Query<MembershipEntryDocument>().ToListAsync();
                Assert.Equal(2, membershipDocs.Count);
            }
        }
    }
}
