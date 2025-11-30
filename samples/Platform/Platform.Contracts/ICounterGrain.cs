using Orleans;

namespace Platform.Contracts
{
    public interface ICounterGrain : IGrainWithStringKey
    {
        ValueTask<int> Get();
        ValueTask<int> Increment();
    }
}
