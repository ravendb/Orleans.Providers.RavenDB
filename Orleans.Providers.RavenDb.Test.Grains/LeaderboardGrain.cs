﻿using Orleans.Providers.RavenDb.Test.GrainInterfaces;
using Orleans.Runtime;

namespace Orleans.Providers.RavenDb.Test.Grains;

public class LeaderboardGrain : Grain, ILeaderboardGrain, IRemindable
{
    private List<PlayerScore> _topPlayers = new();
    private IGrainReminder _reminder;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _reminder = await this.RegisterOrUpdateReminder("UpdateLeaderboard", TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
    }

    public Task<List<PlayerScore>> GetTopPlayers() => Task.FromResult(_topPlayers);

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        var players = await Task.WhenAll(
            Enumerable.Range(1, 10)
                .Select(id => GrainFactory.GetGrain<IGamePlayerGrain>(Guid.NewGuid()).GetScore())
        );

        _topPlayers = players.Select((score, index) => new PlayerScore($"Player {index + 1}", score)).OrderByDescending(p => p.Score).ToList();
    }
}