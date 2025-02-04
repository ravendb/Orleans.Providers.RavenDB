namespace Orleans.Providers.RavenDb.Test.GrainInterfaces;

public interface ILeaderboardGrain : IGrainWithIntegerKey, IRemindable
{
    Task<List<PlayerScore>> GetTopPlayers();

    Task<bool> IsBoardUpdated();
}

[GenerateSerializer]
public record PlayerScore(
    [property: Id(0)] string Name,
    [property: Id(1)] int Score
);