using Orleans;

namespace UnitTests.Grains;

public class PlayerGrain : Grain<PlayerState>, IPlayerGrain
{

    private string playerName = string.Empty;
    private int score = 0;


    public Task<string> GetPlayerName()
    {
        // return Task.FromResult(playerName);
        return Task.FromResult(State.Name);
    }

    public Task<int> GetPlayerScore()
    {
        // return Task.FromResult(score);
        return Task.FromResult(State.Score);
    }

    public Task Win()
    {
        score++;
        State.Score = score;
        return WriteStateAsync();
    }

    public Task Lose()
    {
        score = Int32.Max(score - 1, 0);
        if (State.Score != score)
        {
            State.Score = score;
            return WriteStateAsync();
        }

        return Task.CompletedTask;
    }

    public async Task SetPlayerName(string name)
    {
        playerName = name;
        State.Name = name;
        await WriteStateAsync();
        // return Task.CompletedTask;
    }

    public Task ForceDeactivate()
    {
        DeactivateOnIdle(); // Request immediate deactivation
        return Task.CompletedTask;
    }

    public Task ClearState()
    {
        return ClearStateAsync();
    }
}

public class PlayerState
{
    public string Name { get; set; }
    public int Score { get; set; }
}