using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Runtime;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide;

public class RavenDbMembershipTable : IMembershipTable
{
    private readonly IDocumentStore _documentStore;
    private readonly RavenDbMembershipOptions _options;
    private readonly ILogger<RavenDbMembershipTable> _logger;
    private readonly string _databaseName;
    private readonly string _clusterId;

    public RavenDbMembershipTable(RavenDbMembershipOptions options, ILogger<RavenDbMembershipTable> logger)
    {
        _options = options;
        _logger = logger;
        _databaseName = options.DatabaseName;
        _clusterId = options.ClusterId;
        _documentStore = InitializeDocumentStore();
    }

    private IDocumentStore InitializeDocumentStore()
    {
        try
        {
            var store = new DocumentStore
            {
                Database = _options.DatabaseName,
                Urls = _options.Urls,
                Certificate = _options.Certificate
            };
            store.Initialize();

            // Ensure the database exists
            var dbExists = store.Maintenance.Server.Send(new GetDatabaseRecordOperation(_options.DatabaseName)) != null;
            if (!dbExists)
                store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(_options.DatabaseName)));

            _logger.LogInformation("RavenDB Membership Table DocumentStore initialized successfully.");
            return store;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RavenDB DocumentStore.");
            throw;
        }
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
        try
        {
            using var session = _documentStore.OpenAsyncSession(_databaseName);
            var documents = await session.Query<MembershipEntryDocument>()
                .Where(doc => doc.ClusterId == _clusterId)
                .ToListAsync();

            var entriesWithETags = documents
                .Select(doc => Tuple.Create(ToMembershipEntry(doc), doc.ETag ?? "0"))
                .ToList();

            // Derive the table version
            var tableVersion = entriesWithETags.Any()
                ? new TableVersion(0, entriesWithETags.Max(tuple => tuple.Item2)) // Max ETag for the version
                : new TableVersion(0, "0"); // Default version if no entries

            return new MembershipTableData(entriesWithETags, tableVersion);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }

    public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        var documentId = GetDocumentId(entry.SiloAddress); // Generate the document ID

        using var session = _documentStore.OpenAsyncSession(_databaseName);

        // Check for existing entry to prevent conflicts
        var existing = await session.LoadAsync<MembershipEntryDocument>(documentId);
        if (existing != null)
            return false; // Entry already exists
            //throw new AccessViolationException("failed to insert - entry already exists!");
        if (_options.WaitForIndexesAfterSaveChanges)
            session.Advanced.WaitForIndexesAfterSaveChanges();

        // Simulate table version handling
        var document = ToDocument(entry);
        document.ETag = tableVersion.VersionEtag;

        await session.StoreAsync(document, documentId);
        await session.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        var documentId = GetDocumentId(entry.SiloAddress);

        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var document = await session.LoadAsync<MembershipEntryDocument>(documentId);

        if (document == null || document.ETag != etag)
            return false; // Document doesn't exist or ETag mismatch

        UpdateDocument();

        session.Advanced.UseOptimisticConcurrency = true;
        if (_options.WaitForIndexesAfterSaveChanges)
            session.Advanced.WaitForIndexesAfterSaveChanges();

        try
        {
            await session.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return true;

        void UpdateDocument()
        {
            document.SiloName = entry.SiloName;
            document.SiloAddress = entry.SiloAddress.ToParsableString();
            document.HostName = entry.HostName;
            document.Status = entry.Status.ToString();
            document.ProxyPort = entry.ProxyPort;
            document.StartTime = entry.StartTime.ToUniversalTime();
            document.IAmAliveTime = entry.IAmAliveTime.ToUniversalTime();
            document.RoleName = entry.RoleName;
        }
    }

    public async Task UpdateIAmAlive(MembershipEntry entry)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var documentId = GetDocumentId(entry.SiloAddress); // Convert SiloAddress to string
        var document = await session.LoadAsync<MembershipEntryDocument>(documentId);

        if (document != null)
        {
            document.IAmAliveTime = entry.IAmAliveTime.ToUniversalTime();
            if (_options.WaitForIndexesAfterSaveChanges)
                session.Advanced.WaitForIndexesAfterSaveChanges();

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
            ClusterId = _clusterId,
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

    private string GetDocumentId(SiloAddress siloAddress)
    {
        return $"Membership/{siloAddress.ToParsableString()}";
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

