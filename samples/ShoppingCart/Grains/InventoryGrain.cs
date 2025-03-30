// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Orleans.ShoppingCart.Grains;

[Reentrant]
public sealed class InventoryGrain(
    [PersistentState(
            stateName: "Inventory",
            storageName: "shopping-cart")]
        IPersistentState<InventoryState> state) : Grain, IInventoryGrain
{
    private readonly Dictionary<string, ProductDetails> _productCache = [];

    public override Task OnActivateAsync(CancellationToken _) => PopulateProductCacheAsync();

    Task<HashSet<ProductDetails>> IInventoryGrain.GetAllProductsAsync() =>
        Task.FromResult(_productCache.Values.ToHashSet());

    async Task IInventoryGrain.AddOrUpdateProductAsync(ProductDetails product)
    {
        state.State.Items.Add(product.Id);
        _productCache[product.Id] = product;

        await state.WriteStateAsync();
    }

    public async Task RemoveProductAsync(string productId)
    {
        state.State.Items.Remove(productId);
        _productCache.Remove(productId);

        await state.WriteStateAsync();
    }

    private async Task PopulateProductCacheAsync()
    {
        if (state is not { State.Items.Count: > 0 })
        {
            return;
        }

        await Parallel.ForEachAsync(
            state.State.Items,
            async (id, _) =>
            {
                var productGrain = GrainFactory.GetGrain<IProductGrain>(id);
                _productCache[id] = await productGrain.GetProductDetailsAsync();
            });
    }
}

[GenerateSerializer]
public class InventoryState
{
    public HashSet<string> Items { get; set; } = new HashSet<string>();
}
