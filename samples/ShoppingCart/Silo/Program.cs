// taken from https://github.com/dotnet/samples/tree/main/orleans/ShoppingCart

using Orleans.Providers.RavenDb.Membership;
using Orleans.Providers.RavenDb.StorageProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ShoppingCartService>();
builder.Services.AddSingleton<InventoryService>();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddScoped<ComponentStateChangedObserver>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddLocalStorageServices();

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseOrleans((_, builder) =>
    {
        builder
            .UseLocalhostClustering()
            .UseRavenDbMembershipTable(options =>
            {
                options.DatabaseName = "shopping-cart-db";
                options.Urls = ["http://localhost:8080"];
            })
            .AddRavenDbGrainStorage("shopping-cart", options =>
            {
                options.DatabaseName = "shopping-cart-db";
                options.Urls = ["http://localhost:8080"];
                options.KeyGenerator = (type, id) =>
                {
                    // customize grain state key generator
                    var keyStr = id.Key.ToString();
                    if (keyStr != null && keyStr.StartsWith($"{id.Type}/", StringComparison.InvariantCultureIgnoreCase))
                        return keyStr;
                    return id.ToString();
                };
            })
            .AddStartupTask<SeedProductStoreTask>();
    });
}
else
{
    builder.Host.UseOrleans((context, builder) =>
    {
        var connectionString = context.Configuration["ORLEANS_AZURE_COSMOS_DB_CONNECTION_STRING"]!;

        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "ShoppingCartCluster";
            options.ServiceId = nameof(ShoppingCartService);
        });

        builder
            .UseCosmosClustering(o => o.ConfigureCosmosClient(connectionString))
            .AddCosmosGrainStorage("shopping-cart", o => o.ConfigureCosmosClient(connectionString));
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();
