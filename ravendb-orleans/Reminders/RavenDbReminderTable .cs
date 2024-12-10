using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Runtime;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Indexes;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Orleans.Providers.RavenDB.Reminders
{
    public class RavenDbReminderTable : IReminderTable
    {
        private readonly RavenDbReminderOptions _options;
        private readonly ILogger<RavenDbReminderTable> _logger;
        private IDocumentStore _documentStore;

        public RavenDbReminderTable(RavenDbReminderOptions options, ILogger<RavenDbReminderTable> logger)
        {
            _options = options;
            _logger = logger;

            InitializeDocumentStore();
        }

        private void InitializeDocumentStore()
        {
            try
            {
                _documentStore = new DocumentStore
                {
                    Database = _options.DatabaseName,
                    Urls = _options.Urls,
                    Certificate = _options.Certificate
                };
                _documentStore.Initialize();

                var dbExists = _documentStore.Maintenance.Server.Send(new GetDatabaseRecordOperation(_options.DatabaseName)) != null;
                if (dbExists == false)
                    _documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(_options.DatabaseName)));

                var indexes = _documentStore.Maintenance.Send(new GetIndexNamesOperation(0, int.MaxValue));
                if (indexes.Contains(nameof(ReminderDocumentsByHash)))
                    return;

                new ReminderDocumentsByHash().Execute(_documentStore);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, $"An error occured during initialization of DocumentStore : {e.Message}");
                throw;
            }
        }

        public Task Init() => Task.CompletedTask;
        
        public async Task<ReminderTableData> ReadRows(GrainId grainId)
        {
            using var session = _documentStore.OpenAsyncSession();
            var reminders = await session.Query<RavenDbReminderDocument>()
                                         .Where(r => r.GrainId == grainId.ToString())
                                         .ToListAsync();

            var entries = reminders.Select(r => new ReminderEntry
            {
                GrainId = GrainId.Parse(r.GrainId),
                ReminderName = r.ReminderName,
                StartAt = r.StartAt,
                Period = r.Period
            }).ToList();

            return new ReminderTableData(entries);
        }

        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
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

        public async Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
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

        public async Task<string> UpsertRow(ReminderEntry entry)
        {
            using var session = _documentStore.OpenAsyncSession();
            
            if (_options.WaitForIndexesAfterSaveChanges)
                session.Advanced.WaitForIndexesAfterSaveChanges();
            
            var key = GetKey(entry.GrainId, entry.ReminderName);

            var reminder = await session.LoadAsync<RavenDbReminderDocument>(key) ?? new RavenDbReminderDocument();
            reminder.GrainId = entry.GrainId.ToString();
            reminder.ReminderName = entry.ReminderName;
            reminder.StartAt = entry.StartAt;
            reminder.Period = entry.Period;
            reminder.LastUpdated = DateTime.UtcNow;
            reminder.HashCode = entry.GrainId.GetUniformHashCode();

            await session.StoreAsync(reminder, changeVector: entry.ETag, id: key);
            await session.SaveChangesAsync();

            return session.Advanced.GetChangeVectorFor(reminder);
        }

        public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
        {
            using var session = _documentStore.OpenAsyncSession();
            var key = GetKey(grainId, reminderName);

            var reminder = await session.LoadAsync<RavenDbReminderDocument>(key);
            if (reminder == null) 
                return false;

            if (session.Advanced.GetChangeVectorFor(reminder) != eTag)
                return false; // ETag mismatch
                //throw new InconsistentStateException($"ETag mismatch for {key}");

            session.Delete(reminder);
            await session.SaveChangesAsync();

            return true;
        }

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

        private class ReminderDocumentsByHash : AbstractIndexCreationTask<RavenDbReminderDocument/*, ReminderDocumentsByHash.Result*/>
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
