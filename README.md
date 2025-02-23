# RavenDB-Orleans Provider

[![Build](https://github.com/YOUR_GITHUB_USERNAME/ravendb-orleans/actions/workflows/build.yml/badge.svg)](https://github.com/YOUR_GITHUB_USERNAME/ravendb-orleans/actions)
[![NuGet](https://img.shields.io/nuget/v/RavenDB.Orleans.svg)](https://www.nuget.org/packages/RavenDB.Orleans/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 📌 Overview

**RavenDB-Orleans Provider** is a [Microsoft Orleans](https://dotnet.github.io/orleans/) storage provider that integrates [RavenDB](https://ravendb.net/) as a backing store for grain persistence, reminders, and clustering.

This provider enables **high-performance, distributed applications** that leverage the event-driven, distributed actor model of Orleans with the **scalability and flexibility of RavenDB**.

---

## 🚀 Features
✔ **Grain Storage** – Persist and retrieve Orleans grain states using RavenDB.  
✔ **Reminders Provider** – Stores Orleans timers and reminders in RavenDB.  
✔ **Clustering Support** – Uses RavenDB for **Orleans cluster membership management**.  
✔ **Optimized Queries** – Efficient indexing and querying using **RavenDB’s built-in indexing**.  
✔ **Fully Asynchronous** – Non-blocking, highly scalable storage provider.  
✔ **.NET 9 Support** – Fully compatible with the latest Orleans & .NET 9.  

---

## 🔧 Configuration

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Hosting;
using Orleans.Providers.RavenDb;

// TODO : check this code!

var host = Host.CreateDefaultBuilder()
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseRavenDbClustering(options =>
        {
            options.DatabaseUrl = "http://localhost:8080";
            options.DatabaseName = "OrleansCluster";
        });

        siloBuilder.AddRavenDbGrainStorage("GrainStore", options =>
        {
            options.DatabaseUrl = "http://localhost:8080";
            options.DatabaseName = "OrleansData";
        });

        siloBuilder.UseRavenDbReminderService(options =>
        {
            options.DatabaseUrl = "http://localhost:8080";
            options.DatabaseName = "OrleansReminders";
        });
    })
    .ConfigureServices(services =>
    {
        services.AddLogging();
    })
    .Build();

await host.RunAsync();
```


---

## 📦 Installation

You can install the package via NuGet:

```sh
dotnet add package Orleans.Providers.RavenDb
```

To build the project locally, run:
```sh
pwsh ./build.ps1
```
or (on Linux/macOS)
```sh
./build.sh
```
---

## 📖 Documentation Links
- Official Orleans Documentation: https://dotnet.github.io/orleans/
- RavenDB Documentation: https://ravendb.net/docs/
