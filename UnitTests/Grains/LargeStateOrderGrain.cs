using Orleans.Providers;
using Raven.Client.Documents;

namespace UnitTests.Grains;

[StorageProvider(ProviderName = "GrainStorageForTest")]
public class LargeStateOrderGrain : OrderGrain, ILargeStateOrderGrain
{
    public LargeStateOrderGrain(IDocumentStore store) : base(store)
    {
    }


    public override async Task AddItem(Product product)
    {
        State.Items.Add(product);
        State.TotalPrice += product.Price;

        await WriteStateAsync();
    }
}