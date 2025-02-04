using Orleans.Providers.RavenDb.Test.GrainInterfaces;
using Orleans.Runtime;

namespace Orleans.Providers.RavenDb.Grains;

public class LeaderboardGrain : Grain, ILeaderboardGrain, IRemindable
{
    private List<PlayerScore> _topPlayers = new();
    private bool _updated = false;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        return this.RegisterOrUpdateReminder("UpdateLeaderboard", TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
    }

    public Task<List<PlayerScore>> GetTopPlayers() => Task.FromResult(_topPlayers);
    
    public Task<bool> IsBoardUpdated() => Task.FromResult(_updated);

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        var players = await Task.WhenAll(
            Enumerable.Range(1, 20)
                .Select(id => GrainFactory.GetGrain<IGamePlayerGrain>(id).GetScore())
        );

        _topPlayers = players.Select((score, index) => new PlayerScore($"Player {index + 1}", score))
            .OrderByDescending(p => p.Score)
            .Take(10)
            .ToList();

        _updated = true;
    }
}