using Orleans;

namespace UnitTests.Grains
{
    [Serializable]
    [GenerateSerializer]
    public class UserState
    {
        public UserState()
        {
            Friends = new List<Guid>();
        }

        [Id(0)]
        public string Name { get; set; }
        [Id(1)]
        public string Status { get; set; }
        [Id(2)]
        public List<Guid> Friends { get; set; }
    }

    [Serializable]
    [GenerateSerializer]
    public class DerivedUserState : UserState
    {
        [Id(0)]
        public int Field1 { get; set; }
        [Id(1)]
        public int Field2 { get; set; }
    }

    [Orleans.Providers.StorageProvider(ProviderName = "GrainStorageForTest")]
    public class UserGrainNew : Grain<DerivedUserState>, IUser
    {
        public Task SetName(string name)
        {
            State.Name = name;
            return WriteStateAsync();
        }

        public Task<string> GetName()
        {
            return Task.FromResult(State.Name);
        }

        public async Task AddFriend(Guid key)
        {
            if (State.Friends.Contains(key))
                throw new Exception("Already a friend.");

            State.Friends.Add(key);
            await WriteStateAsync();
        }

        public Task<List<IUser>> GetFriends()
        {
            var friends = State.Friends
                .Select(key => GrainFactory.GetGrain<IUser>(key))
                .ToList();
            return Task.FromResult(friends);
        }
    }
}
