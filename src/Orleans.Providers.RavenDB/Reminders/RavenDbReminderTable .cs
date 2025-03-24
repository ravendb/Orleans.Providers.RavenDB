using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDb.Configuration;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.Exceptions;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Orleans.Providers.RavenDb.Reminders
{
    /// <summary>
    /// Represents the reminder table storage using RavenDB for Orleans reminders.
    /// Provides methods to read, write, update, and remove reminder entries.
    /// </summary>
    public class RavenDbReminderTable : IReminderTable
    {
        private readonly RavenDbReminderOptions _options;
        private readonly ILogger<RavenDbReminderTable> _logger;
        private IDocumentStore _documentStore;
        private readonly Lazy<Task> _initDatabase;

        public RavenDbReminderTable(RavenDbReminderOptions options, ILogger<RavenDbReminderTable> logger)
        {
            _options = options;
            _logger = logger;

            _initDatabase = new Lazy<Task>(InitializeDocumentStoreAsync);
        }

        private async Task InitializeDocumentStoreAsync()
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

                var dbExists = await _documentStore.Maintenance.Server.SendAsync(new GetDatabaseRecordOperation(_options.DatabaseName)) != null;
                if (dbExists == false)
                    await _documentStore.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord(_options.DatabaseName)));

                var indexes = await _documentStore.Maintenance.SendAsync(new GetIndexNamesOperation(0, int.MaxValue));
                if (indexes.Contains(nameof(ReminderDocumentsByHash)))
                    return;

                await new ReminderDocumentsByHash().ExecuteAsync(_documentStore);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, $"An error occured during initialization of DocumentStore : {e.Message}");
                throw;
            }
        }

        public Task Init() => _initDatabase.Value;

        /// <summary>
        /// Reads all reminder rows for the specified grain.
        /// </summary>
        /// <param name="grainId">The grain ID to query for reminders.</param>
        /// <returns>A <see cref="ReminderTableData"/> instance containing all reminders for the grain.</returns>
        public async Task<ReminderTableData> ReadRows(GrainId grainId)
        {
            _logger.LogDebug("Reading reminder rows for GrainId={GrainId}", grainId);
            try
            {
                using var session = _documentStore.OpenAsyncSession();
                var reminders = await session.Advanced.LoadStartingWithAsync<RavenDbReminderDocument>($"reminders/{grainId}/");

                var entries = reminders.Select(r => new ReminderEntry
                {
                    GrainId = GrainId.Parse(r.GrainId),
                    ReminderName = r.ReminderName,
                    StartAt = r.StartAt,
                    Period = r.Period
                }).ToList();

                return new ReminderTableData(entries);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error reading reminder rows for GrainId={GrainId}", grainId);
                throw new OrleansException($"Failed to read rows for grain '{grainId}'", e);

            }
        }

        /// <summary>
        /// Reads all reminders whose hash codes fall within a given range.
        /// </summary>
        /// <param name="begin">The start of the hash range (exclusive).</param>
        /// <param name="end">The end of the hash range (inclusive).</param>
        /// <returns>A <see cref="ReminderTableData"/> with matching reminder entries.</returns>
        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            _logger.LogDebug("Reading reminder rows for Range {BeginHash} to {EndHash}", begin, end);
            try
            {
                using var session = _documentStore.OpenAsyncSession();

                List<RavenDbReminderDocument>? reminders;
                if (begin < end)
                {
                    reminders = await session.Query<RavenDbReminderDocument, ReminderDocumentsByHash>()
                        .Where(doc => (long)doc.HashCode > begin && (long)doc.HashCode <= end)
                        .ToListAsync();
                }
                else
                {
                    reminders = await session.Query<RavenDbReminderDocument, ReminderDocumentsByHash>()
                        .Where(doc => (long)doc.HashCode > begin || (long)doc.HashCode <= end)
                        .ToListAsync();
                }

                var entries = reminders.Select(r => new ReminderEntry
                {
                    GrainId = GrainId.Parse(r.GrainId),
                    ReminderName = r.ReminderName,
                    StartAt = r.StartAt,
                    Period = r.Period,
                    ETag = session.Advanced.GetChangeVectorFor(r)
                }).ToList();

                return new ReminderTableData(entries);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error reading reminder rows for Range {BeginHash} to {EndHash}", begin, end);
                throw new OrleansException($"Failed to read rows for Range [{begin},{end}]", e);
            }

        }

        /// <summary>
        /// Reads a single reminder entry by grain ID and reminder name.
        /// </summary>
        /// <param name="grainId">The grain ID.</param>
        /// <param name="reminderName">The name of the reminder.</param>
        /// <returns>The matching <see cref="ReminderEntry"/> or null if not found.</returns>
        public async Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
        {
            _logger.LogDebug("Reading single reminder row for GrainId={GrainId}, ReminderName={ReminderName}", grainId, reminderName);
            try
            {
                using var session = _documentStore.OpenAsyncSession();
                var key = GetKey(grainId, reminderName);

                var reminder = await session.LoadAsync<RavenDbReminderDocument>(key);
                if (reminder == null)
                    return null;

                var cv = session.Advanced.GetChangeVectorFor(reminder);

                return new ReminderEntry
                {
                    GrainId = grainId,
                    ReminderName = reminderName,
                    StartAt = reminder.StartAt,
                    Period = reminder.Period,
                    ETag = cv
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading single reminder row for GrainId={GrainId}, ReminderName={ReminderName}", grainId, reminderName);
                throw new OrleansException($"Failed to read reminder {reminderName} for grain {grainId}", ex);
            }
        }

        /// <summary>
        /// Adds or updates a reminder row in the database.
        /// </summary>
        /// <param name="entry">The reminder entry to upsert.</param>
        /// <returns>The ETag of the stored document after the operation.</returns>
        public async Task<string> UpsertRow(ReminderEntry entry)
        {
            _logger.LogDebug("Upserting reminder row for GrainId={GrainId}, ReminderName={ReminderName}", entry.GrainId, entry.ReminderName);
            try
            {
                using var session = _documentStore.OpenAsyncSession();

                if (_options.WaitForIndexesAfterSaveChanges)
                    session.Advanced.WaitForIndexesAfterSaveChanges();

                var key = GetKey(entry.GrainId, entry.ReminderName);

                var reminder =  new RavenDbReminderDocument
                {
                    GrainId = entry.GrainId.ToString(),
                    ReminderName = entry.ReminderName,
                    StartAt = entry.StartAt,
                    Period = entry.Period,
                    LastUpdated = DateTime.UtcNow,
                    HashCode = entry.GrainId.GetUniformHashCode()
                };

                await session.StoreAsync(reminder, changeVector: entry.ETag, id: key);
                await session.SaveChangesAsync();

                return session.Advanced.GetChangeVectorFor(reminder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting reminder row for GrainId={GrainId}, ReminderName={ReminderName}", entry.GrainId, entry.ReminderName);
                throw new OrleansException($"Failed to upsert reminder {entry.ReminderName} for grain {entry.GrainId}", ex);
            }

        }

        /// <summary>
        /// Removes a reminder entry if the ETag matches.
        /// </summary>
        /// <param name="grainId">The grain ID.</param>
        /// <param name="reminderName">The reminder name.</param>
        /// <param name="eTag">The expected change vector (ETag).</param>
        /// <returns><c>true</c> if the entry was deleted; <c>false</c> if not found or the ETag did not match.</returns>
        public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
        {
            _logger.LogDebug("Removing reminder row for GrainId={GrainId}, ReminderName={ReminderName}", grainId, reminderName);

            try
            {
                using var session = _documentStore.OpenAsyncSession();
                var key = GetKey(grainId, reminderName);

                session.Delete(key, expectedChangeVector: eTag);
                await session.SaveChangesAsync();

                return true;
            }
            catch (ConcurrencyException)
            {
                // reminder does not exist or ETag mismatch
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reminder row for GrainId={GrainId}, ReminderName={ReminderName}", grainId, reminderName);
                throw new OrleansException($"Failed to remove reminder {reminderName} for grain {grainId}", ex);
            }
        }

        /// <summary>
        /// Test-only method that deletes all reminder entries from the table.
        /// </summary>
        public async Task TestOnlyClearTable()
        {
            using var session = _documentStore.OpenAsyncSession();
            var query = await session.Query<RavenDbReminderDocument>().ToListAsync();
            foreach (var entry in query) 
                session.Delete(entry);
            
            await session.SaveChangesAsync();
        }

        private static string GetKey(GrainId grainId, string reminderName)
        {
            return $"reminders/{grainId}/{reminderName}";
        }

        private class ReminderDocumentsByHash : AbstractIndexCreationTask<RavenDbReminderDocument>
        {
            public ReminderDocumentsByHash()
            {
                Map = documents => from reminderDocument in documents
                    select new
                    {
                        reminderDocument.HashCode
                    };
            }
        }
    }
}
