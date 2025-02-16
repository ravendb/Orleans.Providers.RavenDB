using Orleans;

namespace UnitTests.Grains;

public class CounterGrain : Grain<CounterGrainState>, ICounterGrain
{
    public async Task<int> GetCount()
    {
        await ReadStateAsync();
        return State.Count;
    }

    public async Task Increment()
    {
        State.Count++;
        await WriteStateAsync();
    }
}

public class CounterGrainState
{
    public int Count { get; set; }
}