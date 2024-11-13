using Orleans;
using Orleans.Providers;

namespace UnitTests.Grains;

[StorageProvider(ProviderName = "GrainStorageForTest")]
public class OrderGrain : Grain<OrderState>, IOrderGrain
{
    internal Action ExecuteBetweenReadAndWrite = null;

    public async Task SubmitOrder(Address address)
    {
        //await ReadStateAsync();

        State.ShipTo = address;
        State.Status = OrderStatus.Submitted;

        ExecuteBetweenReadAndWrite?.Invoke();

        await WriteStateAsync();
    }

    public async Task<List<Product>> GetOrderItems()
    {
        await ReadStateAsync();
        return State.Items;
    }

    public async Task<int> GetTotalPrice()
    {
        await ReadStateAsync();
        return State.TotalPrice;
    }

    /*
    public Task ExecuteBetweenReadAndWrite(Action action)
    {
        _executeBetweenReadAndWrite = action;
        return Task.CompletedTask;
    }
    */

    public async Task AddItem(Product product)
    {
        //await ReadStateAsync();

        if (State.Status != OrderStatus.Open)
            throw new InvalidOperationException("Cannot add items to an order that was already submitted");

        ExecuteBetweenReadAndWrite?.Invoke();

        State.Items.Add(product);
        State.TotalPrice += product.Price;

        await WriteStateAsync();
    }
}

[GenerateSerializer]
public class OrderState
{
    [Id(0)]
    public List<Product> Items { get; set; } = new List<Product>();

    [Id(1)]
    public Address ShipTo { get; set; } = new Address();

    [Id(2)]
    public int TotalPrice { get; set; }

    [Id(3)]
    public OrderStatus Status { get; set; }
}

[GenerateSerializer]
public class Product
{
    [Id(0)]
    public string Name { get; set; }

    [Id(1)]
    public int Price { get; set; }
}

[GenerateSerializer]
public class Address
{
    [Id(0)]
    public string Name { get; set; }

    [Id(1)]
    public string Street { get; set; }

    [Id(2)]
    public string City { get; set; }

    [Id(3)]
    public string Country { get; set; }

    [Id(4)]
    public int ZipCode { get; set; }
}

public enum OrderStatus
{
    Open,
    Submitted,
    Shipped
}