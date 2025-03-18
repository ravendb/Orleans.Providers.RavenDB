// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

using Orleans;

namespace UnitTests.Grains;

public interface IInventoryGrain : IGrainWithStringKey
{
    Task<HashSet<ProductDetails>> GetAllProductsAsync();

    Task RemoveProductAsync(string productId);
    Task AddOrUpdateProductAsync(string productId, ProductDetails product);
}