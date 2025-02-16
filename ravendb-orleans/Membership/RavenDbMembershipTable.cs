using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Providers.RavenDB.Membership;
using Orleans.Runtime;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Raven.Client.ServerWide.Operations;
using Raven.Client.ServerWide;
using Raven.Client.Documents.Operations.Indexes;

public class RavenDbMembershipTable : IMembershipTable
{
    private IDocumentStore _documentStore;
    private readonly RavenDbMembershipOptions _options;
    private readonly ILogger<RavenDbMembershipTable> _logger;
    private readonly string _databaseName;
    private readonly string _clusterId;

    public RavenDbMembershipTable(RavenDbMembershipOptions options, ILogger<RavenDbMembershipTable> logger)
    {
        _options = options;
        _logger = logger;
        _databaseName = options.DatabaseName;
        _clusterId = options.ClusterId ?? Guid.NewGuid().ToString();
        //_documentStore = InitializeDocumentStore();
    }

    private void InitializeDocumentStore()
    {
        try
        {
            _documentStore = new DocumentStore
            {
                Database = _options.DatabaseName,
                Urls = _options.Urls,
                Certificate = _options.Certificate,
                Conventions = _options.Conventions
            };
            _documentStore.Initialize();

            // Ensure the database exists
            var dbExists = _documentStore.Maintenance.Server.Send(new GetDatabaseRecordOperation(_options.DatabaseName)) != null;
            if (dbExists == false)
                _documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(_options.DatabaseName)));

            var indexes = _documentStore.Maintenance.Send(new GetIndexNamesOperation(0, int.MaxValue));
            if (indexes.Contains(nameof(MembershipByClusterId)) == false)
                new MembershipByClusterId().Execute(_documentStore);

            _logger.LogInformation("RavenDB Membership Table DocumentStore initialized successfully.");
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
        InitializeDocumentStore();

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
        var docId = GetDocumentId(key);
        var document = await session.LoadAsync<MembershipEntryDocument>(docId, 
            builder => builder.IncludeDocuments<TableVersionDocument>(x => x.ClusterId));

        // Load the TableVersion separately
        var versionDoc = await session.LoadAsync<TableVersionDocument>(_clusterId);
        var tableVersion = versionDoc != null
            ? new TableVersion(versionDoc.Version, session.Advanced.GetChangeVectorFor(versionDoc))
            : new TableVersion(0, "0"); // Default version if no version document exists

        if (document == null)
        {
            return new MembershipTableData([], tableVersion);
        }

        var entry = ToMembershipEntry(document);
        return new MembershipTableData(
            [Tuple.Create(entry, session.Advanced.GetChangeVectorFor(document))],
            tableVersion
        );
    }

    public async Task<MembershipTableData> ReadAll()
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);

        var documents = await session.Query<MembershipEntryDocument, MembershipByClusterId>()
            .Where(doc => doc.ClusterId == _clusterId)
            .Include(x => x.ClusterId)
            .ToListAsync();

        var entriesWithETags = documents
            .Select(doc => Tuple.Create(ToMembershipEntry(doc), session.Advanced.GetChangeVectorFor(doc)))
            .ToList();

        // Load the global table version
        var versionDoc = await session.LoadAsync<TableVersionDocument>(_clusterId);
        var tableVersion = versionDoc != null
            ? new TableVersion(versionDoc.Version, session.Advanced.GetChangeVectorFor(versionDoc))
            : new TableVersion(0, "0"); // Default if no version doc exists

        return new MembershipTableData(entriesWithETags, tableVersion);
    }


    public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);

        // Load the current TableVersion from RavenDB
        var versionDoc = await session.LoadAsync<TableVersionDocument>(_clusterId);
        if (versionDoc == null)
        {
            versionDoc = new TableVersionDocument
            {
                DeploymentId = _clusterId,
                Version = 0
            };
        }

        // Ensure the provided TableVersion is greater than the stored version
        if (tableVersion.Version <= versionDoc.Version)
        {
            _logger.LogWarning("InsertRow failed due to outdated version. Provided={Provided}, Current={Current}",
                tableVersion.Version, versionDoc.Version);
            return false; // Reject insert if version is outdated
        }

        var docId = GetDocumentId(entry.SiloAddress);

        // Check for duplicate silo entry
        var exists = await session.Advanced.ExistsAsync(docId);
        if (exists)
        {
            _logger.LogWarning("InsertRow failed: Duplicate entry for SiloAddress {SiloAddress}", entry.SiloAddress);
            return false;
        }

        session.Advanced.UseOptimisticConcurrency = true;
        if (_options.WaitForIndexesAfterSaveChanges)
            session.Advanced.WaitForIndexesAfterSaveChanges();

        // Insert the new membership entry
        var document = ToDocument(entry);
        document.ClusterId = _clusterId;
        await session.StoreAsync(document, docId);

        // Update the TableVersion
        versionDoc.Version = tableVersion.Version;
        await session.StoreAsync(versionDoc, versionDoc.DeploymentId);

        await session.SaveChangesAsync();

        return true;
    }


    public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        var documentId = GetDocumentId(entry.SiloAddress);
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var document = await session.LoadAsync<MembershipEntryDocument>(documentId, builder => builder.IncludeDocuments(x => x.ClusterId));

        if (document == null || session.Advanced.GetChangeVectorFor(document) != etag)
            return false; // Document doesn't exist or ETag mismatch

        // Load the global TableVersion
        var versionDoc = await session.LoadAsync<TableVersionDocument>(_clusterId);
        if (versionDoc == null)
        {
            return false; // Should never happen, but handle gracefully
        }

        // Ensure the provided TableVersion is greater than the stored version
        if (tableVersion.Version <= versionDoc.Version)
        {
            _logger.LogWarning("UpdateRow failed: outdated table version. Provided={Provided}, Current={Current}",
                tableVersion.Version, versionDoc.Version);
            return false;
        }

        session.Advanced.UseOptimisticConcurrency = true;
        if (_options.WaitForIndexesAfterSaveChanges)
            session.Advanced.WaitForIndexesAfterSaveChanges();

        // Update entry
        UpdateDocument();

        // Update global table version
        versionDoc.Version = tableVersion.Version;

        await session.SaveChangesAsync();

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

        if (document == null)
            return;
        
        document.IAmAliveTime = entry.IAmAliveTime.ToUniversalTime();
        if (_options.WaitForIndexesAfterSaveChanges)
            session.Advanced.WaitForIndexesAfterSaveChanges();

        await session.SaveChangesAsync();
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

        session.Delete(clusterId);

        await session.SaveChangesAsync();
    }

    public async Task CleanupDefunctSiloEntries(DateTimeOffset cutoff)
    {
        using var session = _documentStore.OpenAsyncSession(_databaseName);
        var defunctEntries = await session.Query<MembershipEntryDocument>()
            .Where(e => e.IAmAliveTime < cutoff.UtcDateTime)
            .Include(x => x.ClusterId)
            .ToListAsync();

        foreach (var entry in defunctEntries)
        {
            session.Delete(entry);
        }

        var versionDoc = await session.LoadAsync<TableVersionDocument>(_clusterId);
        versionDoc.Version += 1;


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
            RoleName = document.RoleName,
            SuspectTimes = document.SuspectTimes.Select(x => x.ToTuple()).ToList()
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
            RoleName = entry.RoleName,
            SuspectTimes = entry.SuspectTimes?.Select(SuspectTime.Create).ToList() ?? []
        };
    }

    private string GetDocumentId(SiloAddress siloAddress)
    {
        return $"Membership/{_clusterId}/{siloAddress.ToParsableString()}";
    }

    private class MembershipByClusterId : AbstractIndexCreationTask<MembershipEntryDocument>
    {
        public MembershipByClusterId()
        {
            Map = documents => from membershipDoc in documents
                select new
                {
                    membershipDoc.ClusterId
                };
        }
    }
}



