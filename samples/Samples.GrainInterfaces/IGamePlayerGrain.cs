namespace Samples.GrainInterfaces;

public interface IGamePlayerGrain : IGrainWithIntegerKey
{
    Task SetName(string name);
    Task<string> GetName();
    Task<int> GetScore();
    Task AddScore(int points);
}