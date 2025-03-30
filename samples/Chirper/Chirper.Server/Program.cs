// taken from https://github.com/dotnet/samples/tree/main/orleans/Chirper

using Microsoft.Extensions.Hosting;
using Orleans.Providers.RavenDb.Membership;
using Orleans.Providers.RavenDb.StorageProviders;

Console.Title = "Chirper Server";
const string serverUrl = "http://localhost:8080";
const string databaseName = "chriper";

await Host.CreateDefaultBuilder(args)
    .UseOrleans(
        builder => builder
            .UseLocalhostClustering()
            .UseRavenDbMembershipTable(options =>
            {
                options.Urls = new[] { serverUrl };
                options.DatabaseName = databaseName;
                options.ClusterId = "e4c0f1fd-2297-4a0a-92d6-df5d65ebae47";
            })
            .AddRavenDbGrainStorage("AccountState", options =>
            {
                options.Urls = new[] { serverUrl };
                options.DatabaseName = databaseName;
            }))
    .RunConsoleAsync();
