using Orleans;

namespace UnitTests.Grains
{
    public interface IUser : IGrainWithGuidKey
    {
        Task<string> GetName();
        Task SetName(string name);
        Task AddFriend(Guid key);

        Task<List<IUser>> GetFriends();
    }
}
