using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDb.Configuration;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Orleans.Providers.RavenDb.Membership;

/// <summary>
/// Implements the Orleans <see cref="IMembershipTable"/> interface using RavenDB as the storage provider.
/// Responsible for managing silo membership entries and the global membership table version.
/// </summary>
public class RavenDbMembershipTable : IMembershipTable
{
    private IDocumentStore? _documentStore;
    private readonly RavenDbMembershipOptions _options;
    private readonly ILogger<RavenDbMembershipTable> _logger;
    private readonly string? _databaseName;
    private readonly string _clusterId;
    private readonly string _serviceId;

    public RavenDbMembershipTable(RavenDbMembershipOptions options,
        ILogger<RavenDbMembershipTable> logger)
    {
        _options = options;
        _logger = logger;
        _databaseName = options.DatabaseName;
        _clusterId = options.ClusterId ?? Guid.NewGuid().ToString();
        _serviceId = options.ServiceId ?? _clusterId; // use ClusterId as default ServiceId if not provided
    }

    private void InitializeDocumentStore()
    {
        try
        {
            if (_options.DocumentStore != null)
            {
                _documentStore = _options.DocumentStore;
                _logger.LogInformation("Using externally provided DocumentStore.");
            }
            else
            {
                _documentStore = new DocumentStore
                {
                    Database = _options.DatabaseName,
                    Urls = _options.Urls,
                    Certificate = _options.Certificate,
                    Conventions = _options.Conventions
                }.Initialize();

                _logger.LogInformation("RavenDB Membership Table DocumentStore initialized successfully.");
            }

            if (_options.EnsureDatabaseExists)
            {
                // Ensure the database exists
                var dbExists = _documentStore.Maintenance.Server.Send(new GetDatabaseRecordOperation(_options.DatabaseName)) != null;
                if (dbExists == false)
                    _documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(_options.DatabaseName)));
            }

            var indexes = _documentStore.Maintenance.Send(new GetIndexNamesOperation(0, int.MaxValue));
            if (indexes.Contains(nameof(MembershipByClusterIdAliveTimeStatusAndPort)) == false)
                new MembershipByClusterIdAliveTimeStatusAndPort().Execute(_documentStore);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize RavenDB DocumentStore. Exception={ex}");
            throw;
        }
    }

    public async Task InitializeMembershipTable(bool tryInitTable)
    {
        _logger.LogInformation("Initializing RavenDB Membership Table for ClusterId={ClusterId}", _options.ClusterId);
        try
        {
            InitializeDocumentStore();

            if (tryInitTable)
            {
                using var session = _documentStore!.OpenAsyncSession(_databaseName);
                // Check for existing entries or ensure the database exists
                var count = await session.Query<MembershipEntryDocument, MembershipByClusterIdAliveTimeStatusAndPort>()
                    .Where(x => x.ClusterId == _clusterId && x.ServiceId == _serviceId)
                    .CountAsync();

                _logger.LogInformation($"Existing membership entries: {count}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing RavenDB Membership Table for ClusterId={ClusterId}", _options.ClusterId);
            throw new OrleansException($"Failed to initialize RavenDB Membership Table for ClusterId={_options.ClusterId}. Exception={ex}");
        }

    }

    /// <summary>
    /// Reads the membership entry corresponding to the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The <see cref="SiloAddress"/> of the entry to read.</param>
    /// <returns>The membership data including table version and ETags.</returns>
    public async Task<MembershipTableData> ReadRow(SiloAddress key)
    {
        _logger.LogDebug("Reading membership entry for SiloAddress={SiloAddress}", key);

        try
        {
            var tableVersionDocId = GetTableVersionDocId();

            using var session = _documentStore!.OpenAsyncSession(_databaseName);
            var docId = GetDocumentId(key);
            var document = await session.LoadAsync<MembershipEntryDocument>(docId,
                builder => builder.IncludeDocuments<TableVersionDocument>(x => tableVersionDocId));

            // Load the TableVersion separately
            var versionDoc = await session.LoadAsync<TableVersionDocument>(tableVersionDocId);
            var tableVersion = versionDoc != null
                ? new TableVersion(versionDoc.Version, session.Advanced.GetChangeVectorFor(versionDoc))
                : new TableVersion(0, "0"); // Default version if no version document exists

            if (document == null)
                return new MembershipTableData([], tableVersion);
            
            var entry = ToMembershipEntry(document);
            return new MembershipTableData(
                [Tuple.Create(entry, session.Advanced.GetChangeVectorFor(document))],
                tableVersion
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading membership entry for SiloAddress={SiloAddress}", key);
            throw new OrleansException($"Failed to read membership entry for SiloAddress={key}. Exception={ex}");
        }
    }

    /// <summary>
    /// Reads all membership entries from the table along with the current table version.
    /// </summary>
    public async Task<MembershipTableData> ReadAll()
    {
        _logger.LogDebug("Reading all membership entries for ClusterId={ClusterId}", _options.ClusterId);

        try
        {
            using var session = _documentStore!.OpenAsyncSession(_databaseName);

            var tableVersionDocId = GetTableVersionDocId();

            var documents = await session.Query<MembershipEntryDocument, MembershipByClusterIdAliveTimeStatusAndPort>()
                .Where(doc => doc.ClusterId == _clusterId && doc.ServiceId == _serviceId)
                .Include(x => tableVersionDocId)
                .ToListAsync();

            var entriesWithETags = documents
                .Select(doc => Tuple.Create(ToMembershipEntry(doc), session.Advanced.GetChangeVectorFor(doc)))
                .ToList();

            // Load the global table version
            var versionDoc = await session.LoadAsync<TableVersionDocument>(tableVersionDocId);
            var tableVersion = versionDoc != null
                ? new TableVersion(versionDoc.Version, session.Advanced.GetChangeVectorFor(versionDoc))
                : new TableVersion(0, "0"); // Default if no version doc exists

            return new MembershipTableData(entriesWithETags, tableVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading all membership entries for ClusterId={ClusterId}", _options.ClusterId);
            throw new OrleansException($"Failed to read membership entries for ClusterId={_options.ClusterId}. Exception={ex}");
        }
    }

    /// <summary>
    /// Attempts to insert a new membership entry. Will fail if an entry already exists or version is outdated.
    /// </summary>
    public async Task<bool> InsertRow(MembershipEntry entry, TableVersion tableVersion)
    {
        _logger.LogDebug("Inserting membership entry for SiloAddress={SiloAddress}", entry.SiloAddress);

        try
        {
            using var session = _documentStore!.OpenAsyncSession(_databaseName);

            var versionDocId = GetTableVersionDocId();

            // Load the current TableVersion from RavenDB
            var versionDoc = await session.LoadAsync<TableVersionDocument>(versionDocId) ?? new TableVersionDocument
            {
                DeploymentId = versionDocId,
                Version = 0
            };

            // Ensure the provided TableVersion is greater than the stored version
            if (versionDoc.Version > 0 &&
                tableVersion.Version <= versionDoc.Version)
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
            await session.StoreAsync(versionDoc, versionDocId);

            await session.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting membership entry for SiloAddress={SiloAddress}", entry.SiloAddress);
            throw new OrleansException($"Failed to insert membership entry for SiloAddress={entry.SiloAddress}. Exception={ex}");
        }

    }

    /// <summary>
    /// Updates an existing membership entry if the provided ETag and table version are valid.
    /// </summary>
    public async Task<bool> UpdateRow(MembershipEntry entry, string etag, TableVersion tableVersion)
    {
        _logger.LogDebug("Updating membership entry for SiloAddress={SiloAddress}", entry.SiloAddress);

        try
        {
            var documentId = GetDocumentId(entry.SiloAddress);
            var tableVersionDocId = GetTableVersionDocId();

            using var session = _documentStore!.OpenAsyncSession(_databaseName);
            var document = await session.LoadAsync<MembershipEntryDocument>(documentId, builder => builder.IncludeDocuments(x => tableVersionDocId));

            if (document == null || session.Advanced.GetChangeVectorFor(document) != etag)
                return false; // Document doesn't exist or ETag mismatch

            // Load the global TableVersion
            var versionDoc = await session.LoadAsync<TableVersionDocument>(tableVersionDocId);

            // Ensure the provided TableVersion is greater than the stored version
            if (versionDoc == null || tableVersion.Version <= versionDoc.Version)
            {
                _logger.LogWarning("UpdateRow failed: outdated table version. Provided={Provided}, Current={Current}",
                    tableVersion.Version, versionDoc?.Version);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership entry for SiloAddress={SiloAddress}", entry.SiloAddress);
            throw new OrleansException($"Failed to update membership entry for SiloAddress={entry.SiloAddress}. Exception={ex}");
        }
    }

    /// <summary>
    /// Updates the <c>IAmAliveTime</c> field for the given membership entry's silo.
    /// </summary>
    public async Task UpdateIAmAlive(MembershipEntry entry)
    {
        _logger.LogDebug("Updating IAmAliveTime for SiloAddress={SiloAddress}", entry.SiloAddress);

        try
        {
            using var session = _documentStore!.OpenAsyncSession(_databaseName);
            var documentId = GetDocumentId(entry.SiloAddress); // Convert SiloAddress to string
            var document = await session.LoadAsync<MembershipEntryDocument>(documentId);

            if (document == null)
                return;

            document.IAmAliveTime = entry.IAmAliveTime.ToUniversalTime();
            if (_options.WaitForIndexesAfterSaveChanges)
                session.Advanced.WaitForIndexesAfterSaveChanges();

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating IAmAliveTime for SiloAddress={SiloAddress}", entry.SiloAddress);
            throw new OrleansException($"Failed to update IAmAliveTime for SiloAddress={entry.SiloAddress}. Exception={ex}");
        }

    }

    /// <summary>
    /// Deletes all membership entries for a given cluster.
    /// </summary>
    public async Task DeleteMembershipTableEntries(string clusterId)
    {
        _logger.LogDebug("Deleting membership table entries for ClusterId={ClusterId}", clusterId);

        try
        {
            using var session = _documentStore!.OpenAsyncSession(_databaseName);
            var entriesToDelete = session.Query<MembershipEntryDocument, MembershipByClusterIdAliveTimeStatusAndPort>()
                .Where(e => e.ClusterId == clusterId)
                .ToListAsync();

            foreach (var entry in entriesToDelete.Result)
            {
                session.Delete(entry);
            }

            var tableVersionDocId = $"TableVersion/{_serviceId}/{clusterId}";
            session.Delete(tableVersionDocId);
            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting membership table entries for ClusterId={ClusterId}", clusterId);
            throw new OrleansException($"Failed to delete membership table entries for ClusterId={clusterId}. Exception={ex}");
        }
    }

    /// <summary>
    /// Cleans up entries for silos that are no longer active and whose <c>IAmAliveTime</c> is older than the cutoff.
    /// </summary>
    public async Task CleanupDefunctSiloEntries(DateTimeOffset cutoff)
    {
        _logger.LogDebug("Cleaning up defunct silo entries before cutoff={Cutoff}", cutoff);

        try
        {
            using var session = _documentStore!.OpenAsyncSession(_databaseName);
            var tableVersionDocId = GetTableVersionDocId();

            var defunctEntries = await session.Query<MembershipEntryDocument, MembershipByClusterIdAliveTimeStatusAndPort>()
                .Where(e => e.IAmAliveTime < cutoff.UtcDateTime && e.Status != SiloStatus.Active.ToString())
                 .Include(x => tableVersionDocId)
                .ToListAsync();

            foreach (var entry in defunctEntries)
            {
                session.Delete(entry);
            }

            var versionDoc = await session.LoadAsync<TableVersionDocument>(tableVersionDocId);
            versionDoc.Version += 1;

            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up defunct silo entries before cutoff={Cutoff}", cutoff);
            throw new OrleansException($"Failed to clean up defunct silo entries before cutoff={cutoff}. Exception={ex}");
        }
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
            SuspectTimes = document.SuspectTimes?.Select(x => x.ToTuple()).ToList()
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
            SuspectTimes = entry.SuspectTimes?.Select(SuspectTime.Create).ToList() ?? [],
            ServiceId = _serviceId
        };
    }

    /// <summary>
    /// Generates the document ID for a membership entry based on the silo address.
    /// </summary>
    private string GetDocumentId(SiloAddress siloAddress)
    {
        // For backward compatibility: if ServiceId == ClusterId, use old format
        if (_serviceId == _clusterId)
            return $"Membership/{_clusterId}/{siloAddress.ToParsableString()}";

        // New format to support multiple ServiceIds
        return $"Membership/{_serviceId}/{_clusterId}/{siloAddress.ToParsableString()}";
    }

    /// <summary>
    /// Generates the document ID for the global table version document.
    /// </summary>
    private string GetTableVersionDocId()
    {
        return $"TableVersion/{_serviceId}/{_clusterId}";
    }

    internal class MembershipByClusterIdAliveTimeStatusAndPort : AbstractIndexCreationTask<MembershipEntryDocument>
    {
        public MembershipByClusterIdAliveTimeStatusAndPort()
        {
            Map = documents => from membershipDoc in documents
                select new
                {
                    membershipDoc.ClusterId,
                    membershipDoc.IAmAliveTime,
                    membershipDoc.Status,
                    membershipDoc.ProxyPort,
                    membershipDoc.ServiceId
                };
        }
    }
}