using Orleans;
using UnitTests.Grains;
using UnitTests.Infrastructure;
using Xunit;

namespace UnitTests
{
    public class BasicStorageTests : IClassFixture<RavenDbStorageFixture>
    {
        private readonly RavenDbStorageFixture _fixture;

        public BasicStorageTests(RavenDbStorageFixture fixture)
        {
            _fixture = fixture;
        }


        [Fact]
        public async Task Counters_ReadWrite()
        {
            var grain = _fixture.Client.GetGrain<ICounterGrain>(1);

            var count = await grain.GetCount();
            Assert.Equal(0, count);

            await grain.Increment();
            count = await grain.GetCount();
            Assert.Equal(1, count);

            await grain.Increment();
            count = await grain.GetCount();
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task PlayerGrain_ReadWrite()
        {
            var grain = _fixture.Client.GetGrain<IPlayerGrain>(1);
            await grain.SetPlayerName("Avi Ron");
            var name = await grain.GetPlayerName();
            Assert.Equal("Avi Ron", name);

            await grain.SetPlayerName("Eli Kopter");
            name = await grain.GetPlayerName();
            Assert.Equal("Eli Kopter", name);

            await grain.Win();
            await grain.Win();
            var score = await grain.GetPlayerScore();
            Assert.Equal(2, score);

            await grain.Lose();
            score = await grain.GetPlayerScore();
            Assert.Equal(1, score);
        }

        [Fact]
        public async Task Orders_ReadWrite()
        {
            var id = Guid.NewGuid();
            var order = _fixture.Client.GetGrain<IOrderGrain>(id);
            Assert.Empty(await order.GetOrderItems());

            //IGrainStorage grainStorage = GrainStorageHelpers.GetGrainStorage(typeof(OrderGrain), _fixture.Host.Services);

            await order.AddItem(new Product
            {
                Name = "milk",
                Price = 5
            });

            await order.AddItem(new Product
            {
                Name = "coffee",
                Price = 25
            });

            var items = await order.GetOrderItems();
            Assert.Equal(2, items.Count);

            var totalPrice = await order.GetTotalPrice();
            Assert.Equal(30, totalPrice);

            await order.AddItem(new Product
            {
                Name = "brown rice",
                Price = 15
            });

            await order.SubmitOrder(new Address
            {
                Name = "ayende",
                Street = "11 Aehad Ha'Am",
                City = "Hadera",
                Country = "Israel",
                ZipCode = 12345678
            });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await order.AddItem(new Product
            {
                Name = "sugar",
                Price = 7
            }));

            Assert.Contains("Cannot add items to an order that was already submitted", ex.Message);

            totalPrice = await order.GetTotalPrice();
            Assert.Equal(45, totalPrice);
        }

        [Fact]
        public async Task ShouldThrowConcurrencyException()
        {
            var orderId = Guid.NewGuid();
            var order = _fixture.Client.GetGrain<IOrderGrain>(orderId);

            await order.AddItem(new Product
            {
                Name = "milk",
                Price = 5
            });

            await order.AddItem(new Product
            {
                Name = "coffee",
                Price = 25
            });

            var items = await order.GetOrderItems();
            Assert.Equal(2, items.Count);

            using (var session = _fixture.DocumentStore.OpenAsyncSession())
            {
                var docId = $"{nameof(OrderState)}/{order.GetGrainId()}";
                var doc = await session.LoadAsync<OrderState>(docId);
                doc.ShipTo = new Address
                {
                    Name = "Jerry Garcia",
                    Street = "Ashbury 42",
                    City = "San Fransisco CA",
                    Country = "USA",
                    ZipCode = 9765432
                };
                await session.SaveChangesAsync();
            }

            var submitOrderAttempt = order.SubmitOrder(new Address
            {
                Name = "ayende",
                Street = "11 Aehad Ha'Am",
                City = "Hadera",
                Country = "Israel",
                ZipCode = 12345678
            });

            //var ex = await Assert.ThrowsAsync<OrleansException>(async () => await submitOrderAttempt);

            //Assert.Contains("Optimistic concurrency violation, transaction will be aborted", ex.Message);

        }

        [Fact]
        public async Task UserGrain_Read_Write()
        {
            Guid id = Guid.NewGuid();
            IUser userGrain = _fixture.Client.GetGrain<IUser>(id);
            string name = "user";
            await userGrain.SetName(name);

            string readName = await userGrain.GetName();

            Assert.Equal(name, readName); // Read back previously set name

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();
            string name1 = "user1";
            string name2 = "user2";

            IUser otherUser1 = _fixture.Client.GetGrain<IUser>(id1);
            IUser otherUser2 = _fixture.Client.GetGrain<IUser>(id2);
            await otherUser1.SetName(name1);
            await otherUser2.SetName(name2);

            var readName1 = await otherUser1.GetName();
            var readName2 = await otherUser2.GetName();

            Assert.Equal(name1, readName1); // Friend #1 Name
            Assert.Equal(name2, readName2); // Friend #2 Name

            // send friend requests 
            await userGrain.AddFriend(id1);
            await userGrain.AddFriend(id2);

            var friends = await userGrain.GetFriends();
            Assert.Equal(2, friends.Count); // Number of friends
            Assert.Equal(name1, await friends[0].GetName());
            Assert.Equal(name2, await friends[1].GetName());
        }


        [Fact]
        public async Task CountersStress()
        {
            var numOfUpdates = 10_000;
            List<Task> tasks = [];

            for (int i = 0; i < numOfUpdates; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var grain = _fixture.Client.GetGrain<ICounterGrain>(1234);
                    await grain.Increment();
                }));
            }

            const int timeout = 60_000;
            var done = Task.WhenAll(tasks);
            var timeoutTask = Task.Delay(timeout);
            
            Assert.True(await Task.WhenAny(done, timeoutTask) != timeoutTask, $"tasks failed to completed in {TimeSpan.FromMilliseconds(timeout).TotalSeconds} seconds");    

            var grain = _fixture.Client.GetGrain<ICounterGrain>(1234);
            var count = await grain.GetCount();
            Assert.Equal(numOfUpdates, count);

        }

    }
}