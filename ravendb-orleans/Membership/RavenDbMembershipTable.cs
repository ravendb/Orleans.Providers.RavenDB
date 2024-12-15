using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Runtime;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

public class RavenDbMembershipTable : IMembershipTable
{
    private readonly IDocumentStore _documentStore;
    private readonly string _databaseName;
    private readonly ILogger<RavenDbMembershipTable> _logger;

    public RavenDbMembershipTable(IOptions<RavenDbMembershipOptions> options, IDocumentStore documentStore, ILogger<RavenDbMembershipTable> logger)
    {
        _documentStore = documentStore;
        _databaseName = options.Value.DatabaseName;
        _logger = logger;
    }

    public async Task InitializeMembershipTable(bool tryInitTable)
    {
        _logger.LogInformation("Initializing RavenDB Membership Table");
        if (tryInitTable)
        {
            using var session = _documentStore.OpenAsyncSession(_databaseName);
            // Check for existing entries or ensure the database exists
            var count = await session.Query<MembershipEntryDocument>().CountAsync();
            _logger.LogInformation($"Existing membership entries: {count}");
        }
    }

    public async Task<MembershipTableData> ReadRow(SiloAddress key)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var document = await session.Query<MembershipEntryDocument>()
            .FirstOrDefaultAsync(d => d.SiloAddress == key.ToParsableString());

        if (document == null)
        {
            return new MembershipTableData(new List<Tuple<MembershipEntry, string>>(), new TableVersion(0, "0"));
        }

        var entry = ToMembershipEntry(document);
        return new MembershipTableData(
            [Tuple.Create(entry, document.ETag ?? "0")],
            new TableVersion(0, document.ETag ?? "0")
        );
    }

    public async Task<MembershipTableData> ReadAll()
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var documents = await session.Query<MembershipEntryDocument>().ToListAsync();

        var entriesWithETags = documents
            .Select(doc => Tuple.Create(ToMembershipEntry(doc), doc.ETag ?? "0"))
            .ToList();

        // Derive the table version
        var tableVersion = entriesWithETags.Any()
            ? new TableVersion(0, entriesWithETags.Max(tuple => tuple.Item2)) // Max ETag for the version
            : new TableVersion(0, "0"); // Default version if no entries

        return new MembershipTableData(entriesWithETags, tableVersion);
    }

    public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var document = ToDocument(entry);

        // Simulate table version handling
        document.ETag = tableVersion.VersionEtag;

        await session.StoreAsync(document);
        await session.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var documentId = GetDocumentId(entry.SiloAddress.ToParsableString()); // Convert SiloAddress to string
        var document = await session.LoadAsync<MembershipEntryDocument>(documentId);

        if (document == null || document.ETag != etag)
        {
            return false;
        }

        document = ToDocument(entry);
        session.Advanced.UseOptimisticConcurrency = true;
        await session.StoreAsync(document);
        await session.SaveChangesAsync();

        return true;
    }

    public async Task UpdateIAmAlive(MembershipEntry entry)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var documentId = GetDocumentId(entry.SiloAddress.ToParsableString()); // Convert SiloAddress to string
        var document = await session.LoadAsync<MembershipEntryDocument>(documentId);

        if (document != null)
        {
            document.IAmAliveTime = entry.IAmAliveTime.ToUniversalTime();
            await session.SaveChangesAsync();
        }
    }

    public async Task DeleteMembershipTableEntries(string clusterId)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var entriesToDelete = session.Query<MembershipEntryDocument>()
            .Where(e => e.ClusterId == clusterId)
            .ToListAsync();

        foreach (var entry in entriesToDelete.Result)
        {
            session.Delete(entry);
        }

        await session.SaveChangesAsync();
    }

    public async Task CleanupDefunctSiloEntries(DateTimeOffset cutoff)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var defunctEntries = await session.Query<MembershipEntryDocument>()
            .Where(e => e.IAmAliveTime < cutoff.UtcDateTime)
            .ToListAsync();

        foreach (var entry in defunctEntries)
        {
            session.Delete(entry);
        }

        await session.SaveChangesAsync();
    }

    private MembershipEntry ToMembershipEntry(MembershipEntryDocument document)
    {
        return new MembershipEntry
        {
            SiloName = document.SiloName,
            SiloAddress = SiloAddress.FromParsableString(document.SiloAddress),
            HostName = document.HostName,
            Status = Enum.Parse<SiloStatus>(document.Status),
            ProxyPort = document.ProxyPort,
            StartTime = document.StartTime,
            IAmAliveTime = document.IAmAliveTime,
            RoleName = document.RoleName
        };
    }

    private MembershipEntryDocument ToDocument(MembershipEntry entry)
    {
        return new MembershipEntryDocument
        {
            SiloName = entry.SiloName,
            SiloAddress = entry.SiloAddress.ToParsableString(),
            HostName = entry.HostName,
            Status = entry.Status.ToString(),
            ProxyPort = entry.ProxyPort,
            StartTime = entry.StartTime.ToUniversalTime(),
            IAmAliveTime = entry.IAmAliveTime.ToUniversalTime(),
            RoleName = entry.RoleName
        };
    }

    private string GetDocumentId(string siloAddress)
    {
        return $"Membership/{siloAddress}";
    }
}

public class MembershipEntryDocument
{
    public string ClusterId { get; set; }
    public string SiloName { get; set; }
    public string SiloAddress { get; set; }
    public string HostName { get; set; }
    public string Status { get; set; }
    public int ProxyPort { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime IAmAliveTime { get; set; }
    public string RoleName { get; set; }
    public string ETag { get; set; } // Used for concurrency control
}

