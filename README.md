# RavenDB-Orleans Provider

[![Build](https://github.com/ravendb/Orleans.Providers.RavenDB/actions/workflows/orleansTests.yml/badge.svg)](https://github.com/ravendb/Orleans.Providers.RavenDB/actions/workflows/orleansTests.yml)
[![NuGet](https://img.shields.io/nuget/v/Orleans.Providers.RavenDB.svg)](https://www.nuget.org/packages/Orleans.Providers.RavenDB/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

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
✔ **.NET 8 Support** – Fully compatible with the latest Orleans & .NET 8.  

---

## 🔧 Configuration

```csharp

var host = Host.CreateDefaultBuilder(args)
	.UseOrleans(siloBuilder =>
	{
		// Define RavenDB connection settings
		string serverUrl = "http://localhost:8080";
		string databaseName = "OrleansDemo";

		siloBuilder
			// Configure clustering with RavenDB
			.UseRavenDbMembershipTable(options =>
			{
				options.Urls = new[] { serverUrl };
				options.DatabaseName = databaseName;
			})
			// Configure grain storage with RavenDB
			.AddRavenDbGrainStorage("RavenGrainStorage", options =>
			{
				options.Urls = new[] { serverUrl };
				options.DatabaseName = databaseName;
			})
			// Configure reminders with RavenDB
			.AddRavenDbReminderTable(options =>
			{
				options.Urls = new[] { serverUrl };
				options.DatabaseName = databaseName;
			})
			// Configure other Orleans settings
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
			.ConfigureLogging(logging => logging.AddConsole());
	}).Build();

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
