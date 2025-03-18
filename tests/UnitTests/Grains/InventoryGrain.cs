// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace UnitTests.Grains;

[Reentrant]
public sealed class InventoryGrain(
    [PersistentState(
        stateName: "Inventory",
        storageName: "GrainStorageForTest")]
    IPersistentState<InventoryState> state) : Grain, IInventoryGrain
{
    private readonly Dictionary<string, ProductDetails> _productCache = [];

   // public override Task OnActivateAsync(CancellationToken _) => PopulateProductCacheAsync();

    Task<HashSet<ProductDetails>> IInventoryGrain.GetAllProductsAsync() =>
        Task.FromResult(_productCache.Values.ToHashSet());

    async Task IInventoryGrain.AddOrUpdateProductAsync(string id, ProductDetails product)
    {
        state.State.Items.Add(id);
        product.Id = id;
        _productCache[id] = product;

        await state.WriteStateAsync();
    }

    public async Task RemoveProductAsync(string productId)
    {
        state.State.Items.Remove(productId);
        _productCache.Remove(productId);

        await state.WriteStateAsync();
    }

    //private async Task PopulateProductCacheAsync()
    //{
    //    if (state is not { State.Items.Count: > 0 })
    //    {
    //        return;
    //    }

    //    await Parallel.ForEachAsync(
    //        state.State.Items,
    //        async (id, _) =>
    //        {
    //            var productGrain = GrainFactory.GetGrain<IProductGrain>(id);
    //            _productCache[id] = await productGrain.GetProduct();
    //        });
    //}
}

[GenerateSerializer]
public class InventoryState
{
    public HashSet<string> Items { get; set; } = new HashSet<string>();
}