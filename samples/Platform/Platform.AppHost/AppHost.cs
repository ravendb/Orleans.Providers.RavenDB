using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var seq = builder.AddSeq("seq", 5341)
    .ExcludeFromManifest()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var ravenServer = builder
    .AddRavenDB("ravenServer")
    .WithImageTag("latest")
    .WithDataVolume();

var ravendb = ravenServer.AddDatabase("CounterStore");

var orleans = builder.AddOrleans("orleans")
    .WithClustering(ravendb)
    .WithGrainStorage(ravendb)
    .WithReminders(ravendb);

var silo = builder.AddProject<Platform_Silo>("silo")
    .WithReference(orleans)
    .WithReference(ravendb)
    .WaitFor(ravendb)
    .WithReference(seq)
    .WaitFor(seq)
    .WithReplicas(3);

var apiService = builder.AddProject<Platform_ApiService>("apiservice")
    .WithReference(orleans.AsClient())
    .WithReference(ravendb)
    .WaitFor(ravendb)
    .WaitFor(silo)
    .WithReference(seq)
    .WaitFor(seq)
    .WithHttpHealthCheck("/health");

builder.AddProject<Platform_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
