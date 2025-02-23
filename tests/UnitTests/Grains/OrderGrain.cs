using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;

namespace UnitTests.Grains;

[StorageProvider(ProviderName = "GrainStorageForTest")]
public class OrderGrain : Grain<OrderState>, IOrderGrain
{
    private readonly IDocumentStore _store;

    private string _script;

    public OrderGrain(IDocumentStore store)
    {
        _store = store;
    }

    public async Task SubmitOrder(Address address)
    {
        await ReadStateAsync();

        State.ShipTo = address;
        State.Status = OrderStatus.Submitted;

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

    public Task ClearState()
    {
        return ClearStateAsync();
    }

    public Task OnBeforeWriteStateAsync(string script)
    {
        _script = script;

        return Task.CompletedTask;
    }

    public virtual async Task AddItem(Product product)
    {
        await ReadStateAsync();

        if (State.Status != OrderStatus.Open)
            throw new InvalidOperationException("Cannot add items to an order that was already submitted");

        State.Items.Add(product);
        State.TotalPrice += product.Price;

        if (string.IsNullOrEmpty(_script) == false)
        {

            try
            {
                var docId = $"{nameof(OrderState)}/{this.GetGrainId()}";

                await _store.Operations.SendAsync(new PatchOperation(docId, changeVector: null, new PatchRequest
                {
                    Script = _script
                }));
            }
            catch (Exception e)
            {
                throw new OrleansException(e.Message);
            }

        }

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