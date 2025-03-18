using Orleans;
using Orleans.Runtime;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnitTests.Grains;

public class ProductGrain : Grain, IProductGrain
{
    private readonly IPersistentState<ProductDetails> _state;

    public ProductGrain([PersistentState("product", "GrainStorageForTest")] IPersistentState<ProductDetails> state)
    {
        _state = state;
    }

    public async Task SetProduct(ProductDetails product)
    {
        _state.State = product;
        await _state.WriteStateAsync();


        var inventoryGrain = GrainFactory.GetGrain<IInventoryGrain>("shop");
        var primaryKeyString = this.GetPrimaryKeyString();

        await inventoryGrain.AddOrUpdateProductAsync(primaryKeyString, product);
    }

    public Task<ProductDetails> GetProduct()
    {
        return Task.FromResult(_state.State);
    }

    public async Task ForceDeactivate()
    {
        DeactivateOnIdle();
        await Task.CompletedTask;
    }
}

[GenerateSerializer, Immutable]
public sealed record class ProductDetails
{
    [Id(0)] public string Id { get; set; } = "123";
    [Id(1)] public string Name { get; set; } = null!;
    [Id(2)] public string Description { get; set; } = null!;
    [Id(4)] public int Quantity { get; set; }
    [Id(5)] public decimal UnitPrice { get; set; }

}