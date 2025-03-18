using Orleans;
using System.Threading.Tasks;
using UnitTests.Grains;

public interface IProductGrain : IGrainWithStringKey
{
    Task SetProduct(ProductDetails product);
    Task<ProductDetails> GetProduct();
    Task ForceDeactivate();
}