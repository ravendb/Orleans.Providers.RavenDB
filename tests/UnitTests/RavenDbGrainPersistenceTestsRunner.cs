using TestExtensions;
using TestExtensions.Runners;
using Xunit.Abstractions;

namespace UnitTests
{
    public class RavenDbGrainPersistenceTestsRunner(
        ITestOutputHelper output,
        BaseTestClusterFixture fixture,
        string grainNamespace = "UnitTests.Grains")
        : GrainPersistenceTestsRunner(output, fixture, grainNamespace);
}
