using TestExtensions.Runners;
using UnitTests.Infrastructure;
using Xunit.Abstractions;

namespace UnitTests.Infrastructure;

internal sealed class RavenDbGrainPersistenceTestsRunner : GrainPersistenceTestsRunner
{
    public RavenDbGrainPersistenceTestsRunner(ITestOutputHelper output, RavenDbStorageFixture fixture, string grainNamespace = "UnitTests.Grains")
        : base(output, fixture, grainNamespace)
    {
    }
}
