using TestExtensions;
using UnitTests;
using Xunit;

[CollectionDefinition(TestEnvironmentFixture.DefaultCollection)]
public class RavenDbTestsCollection : ICollectionFixture<ConnectionStringFixture>, ICollectionFixture<TestEnvironmentFixture>
{
    // This class has no code, and is never instantiated.
    // Its purpose is simply to attach the fixtures to the test collection.
}