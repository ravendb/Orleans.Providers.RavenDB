using Orleans;

namespace UnitTests.Grains;

public interface IOrderGrain : IGrainWithGuidKey
{
    Task SubmitOrder(Address address);

    Task AddItem(Product product);

    Task<List<Product>> GetOrderItems();

    Task<int> GetTotalPrice();

    Task ClearState();

    Task OnBeforeWriteStateAsync(string script);
}

public interface ILargeStateOrderGrain : IOrderGrain
{}
