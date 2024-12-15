using Orleans;

namespace UnitTests.Grains;

public interface IOrderGrain : IGrainWithGuidKey
{
    Task SubmitOrder(Address address);

    Task AddItem(Product product);

    Task<List<Product>> GetOrderItems();

    Task<int> GetTotalPrice();

    Task ClearState();
}

public interface ITestHook
{
    /// <summary>
    /// A hook to be invoked before WriteStateAsync in the grain.
    /// </summary>
    Func<Task> OnBeforeWriteStateAsync { get; set; }
}

public class DefaultTestHook : ITestHook
{
    public Func<Task> OnBeforeWriteStateAsync { get; set; } = () => Task.CompletedTask;
}