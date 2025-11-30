using Platform.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSeqEndpoint(connectionName: "seq");

// Let Aspire configure Orleans based on env vars (ProviderType etc.)
builder.UseOrleans();

var app = builder.Build();

app.MapGet("/", () => "OK");

await app.RunAsync();

public sealed class CounterState
{
    public int Value { get; set; }
}

public sealed class CounterGrain : ICounterGrain
{
    private readonly IPersistentState<CounterState> _state;

    public CounterGrain([PersistentState(stateName: "counter", storageName: "CounterStore")] IPersistentState<CounterState> state)
        => _state = state;
    public ValueTask<int> Get()
    {
        return ValueTask.FromResult(_state.State.Value);
    }

    public async ValueTask<int> Increment()
    {
        _state.State.Value++;
        await _state.WriteStateAsync();
        return _state.State.Value;
    }
}