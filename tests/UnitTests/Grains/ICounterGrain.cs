using Orleans;

namespace UnitTests.Grains;

public interface ICounterGrain : IGrainWithIntegerKey
{
    Task<int> GetCount();
    Task Increment();
}