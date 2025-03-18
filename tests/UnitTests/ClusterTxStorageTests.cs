using UnitTests.Grains;
using UnitTests.Infrastructure;
using Xunit;
using Orleans.Runtime;

namespace UnitTests
{
    public class ClusterTxStorageTests : IClassFixture<RavenDbClusterTxStorageFixture>
    {
        private readonly RavenDbClusterTxStorageFixture _fixture;

        public ClusterTxStorageTests(RavenDbClusterTxStorageFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GrainStorage_ShouldHandleConcurrentWrites()
        {
            var grainIds = Enumerable.Range(1, 100).Select(id => _fixture.Client.GetGrain<ICounterGrain>(id)).ToList();

            var tasks = grainIds.Select(async grain =>
            {
                await grain.Increment();
                return await grain.GetCount();
            });

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                Assert.Equal(1, result); // Ensure each grain updated independently
            }
        }

        [Fact]
        public async Task GrainStorage_ShouldHandleInvalidETag()
        {
            var grainId = Guid.NewGuid();
            var grain = _fixture.Client.GetGrain<IOrderGrain>(grainId);

            await grain.AddItem(new Product { Name = "Test Product", Price = 100 });
            
            // Simulate external modification between read & write
            await grain.OnBeforeWriteStateAsync(script: "this.TotalPrice += 5");

            // Attempt to modify the grain state 
            var exception = await Assert.ThrowsAsync<OrleansException>(async () =>
            {
                await grain.AddItem(new Product { Name = "Product2", Price = 15 });
            });

            Assert.Contains("Optimistic concurrency violation", exception.Message);
        }

        [Fact]
        public async Task GrainStorage_CountersReadWrite()
        {
            var grain = _fixture.Client.GetGrain<ICounterGrain>(1000);

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
        public async Task GrainStorage_PlayerGrainReadWrite()
        {
            var grain = _fixture.Client.GetGrain<IPlayerGrain>(2);
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
        public async Task GrainStorage_OrdersReadWrite()
        {
            var id = Guid.NewGuid();
            var order = _fixture.Client.GetGrain<IOrderGrain>(id);
            Assert.Empty(await order.GetOrderItems());

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
        public async Task GrainStorage_ShouldHandleHighThroughput()
        {
            int numOperations = 5000;
            var start = 10_000;
            var tasks = Enumerable.Range(start, numOperations)
                .Select(async i =>
                {
                    var grain = _fixture.Client.GetGrain<ICounterGrain>(i);
                    await grain.Increment();
                });

            await Task.WhenAll(tasks);

            foreach (var i in Enumerable.Range(start, numOperations))
            {
                var grain = _fixture.Client.GetGrain<ICounterGrain>(i);
                var count = await grain.GetCount();
                Assert.Equal(1, count);
            }
        }
    }
}