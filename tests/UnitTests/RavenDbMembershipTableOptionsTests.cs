using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Providers.RavenDb.Membership;
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
    }
}
