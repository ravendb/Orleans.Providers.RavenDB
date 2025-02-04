using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.RavenDb.Grains;
using Orleans.Providers.RavenDB.Reminders;
using Orleans.Providers.RavenDB.StorageProviders;
using Orleans.Providers.RavenDb.Test.GrainInterfaces;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace DemoApp;

public class Program
{
    private static async Task DeleteOldDataIfNeeded()
    {
        const string databaseName = "OrleansDemo";
        using var store = new DocumentStore()
        {
            Database = databaseName,
            Urls = ["http://localhost:8080"]
        }.Initialize();

        var dbRec = await store.Maintenance.Server.SendAsync(new GetDatabaseRecordOperation(databaseName));
        if (dbRec != null)
        {
            await store.Maintenance.Server.SendAsync(new DeleteDatabasesOperation(databaseName, hardDelete: true));
            await Task.Delay(5000);

            await store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(databaseName)));

        }
    }

    public static async Task Main(string[] args)
    {
        var grainAssembly = typeof(GamePlayerGrain).Assembly;
        Console.WriteLine($"Loading grains from: {grainAssembly.FullName}");

        //var grainAssembly2 = typeof(IGamePlayerGrain).Assembly;
        //Console.WriteLine($"Also, Loading grains from: {grainAssembly2.FullName}");

        await DeleteOldDataIfNeeded();

        using var cts = new CancellationTokenSource();

        using var host = CreateHost();
        var hostTask = host.StartAsync(cts.Token); // Start silo in background

        // Wait extra time to ensure silo is ready
        await Task.Delay(5000);

        using var clientHost = CreateClient();
        await TryConnectClient(clientHost);

        var client = clientHost.Services.GetRequiredService<IClusterClient>();
        await RunClient(client);

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();

        await clientHost.StopAsync();
        cts.Cancel();
        await hostTask;
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .UseOrleans(siloBuilder =>
            {
                string serverUrl = "http://localhost:8080";
                string databaseName = "OrleansDemo";

                siloBuilder.UseLocalhostClustering()
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = "dev";
                        options.ServiceId = "OrleansDemo";
                    })
                    .Configure<EndpointOptions>(options =>
                    {
                        options.AdvertisedIPAddress = IPAddress.Loopback;
                        options.SiloPort = 11111;
                        options.GatewayPort = 30000;
                    })
                    .ConfigureLogging(logging => logging.AddConsole())
                    .UseRavenDbMembershipTable(options =>
                    {
                        options.Urls = new[] { serverUrl };
                        options.DatabaseName = databaseName;
                    })
                    .AddRavenDbGrainStorageAsDefault(options =>
                    {
                        options.Urls = new[] { serverUrl };
                        options.DatabaseName = databaseName;
                    })
                    .AddRavenDbReminderTable(options =>
                    {
                        options.Urls = new[] { serverUrl };
                        options.DatabaseName = databaseName;
                    });
            })
            .Build();
    }

    private static IHost CreateClient()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddOrleansClient(client =>
                {
                    client.UseLocalhostClustering(gatewayPort: 30000)
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "OrleansDemo";
                        })
                        .Configure<ConnectionOptions>(options =>
                        {
                            options.OpenConnectionTimeout = TimeSpan.FromSeconds(15);
                        });
                });
            })
            .Build();
    }

    private static async Task TryConnectClient(IHost clientHost)
    {
        var client = clientHost.Services.GetRequiredService<IClusterClient>();

        for (int i = 0; i < 5; i++)
        {
            try
            {
                Console.WriteLine("Attempting to connect to Orleans cluster...");
                await clientHost.StartAsync();
                Console.WriteLine("Client connected.");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client connection attempt {i + 1} failed: {ex.Message}");
                await Task.Delay(3000);
            }
        }

        throw new Exception("Failed to connect to Orleans cluster after multiple attempts.");
    }

    private static async Task RunClient(IClusterClient client)
    {
        var leaderboard = client.GetGrain<ILeaderboardGrain>(0);

        var rand = new Random(12345666);

        for (int i = 1; i <= 20; i++)
        {
            var playerGrain = client.GetGrain<IGamePlayerGrain>(i);
            await playerGrain.SetName("JohnDoe-" + i);

            var score = rand.Next(100);
            await playerGrain.AddScore(score);

            Console.WriteLine($"Player {await playerGrain.GetName()} has score: {await playerGrain.GetScore()}");

            await Task.Delay(10);
        }


        // wait for reminder
        var sw = Stopwatch.StartNew();
        bool updated = false;
        while (sw.Elapsed < TimeSpan.FromMinutes(1))
        {
            updated = await leaderboard.IsBoardUpdated();
            if (updated)
                break;

            await Task.Delay(1000);
        }

        if (updated == false)
            throw new Exception("leaderboard grain is not updated with player scores after waiting for 60 seconds");

        
        var topPlayers = await leaderboard.GetTopPlayers();

        Console.WriteLine("\n Leaderboard:");
        for (var i = 0; i < topPlayers.Count; i++)
        {
            var player = topPlayers[i];
            Console.WriteLine($"({i+1}) {player.Name}: {player.Score} points");
        }
    }
}