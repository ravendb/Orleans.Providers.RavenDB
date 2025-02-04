using Orleans.Providers.RavenDb.Test.GrainInterfaces;

public class PlayerGrain : Grain<PlayerState>, IGamePlayerGrain
{
    public Task SetName(string name)
    {
        State.Name = name;
        return WriteStateAsync();
    }

    public Task<string> GetName() => Task.FromResult(State.Name);

    public Task<int> GetScore() => Task.FromResult(State.Score);

    public Task AddScore(int points)
    {
        State.Score += points;
        return WriteStateAsync();
    }
}

[GenerateSerializer]
public class PlayerState
{
    [Id(0)]
    public string Name { get; set; }

    [Id(1)]
    public int Score { get; set; }
}