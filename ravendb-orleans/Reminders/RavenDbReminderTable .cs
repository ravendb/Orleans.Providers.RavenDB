using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Runtime;
using Orleans.Storage;
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
            if (indexes.Contains("ReminderDocument/ByHash"))
                return;

            try
            {
                new ReminderDocument_ByHash().Execute(_documentStore);
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e.Message);
                Console.WriteLine(e);
                throw;
            }
        }

        public Task Init()
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

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
                Period = r.Period,
                //ETag = r.ETag
            }).ToList();

            return new ReminderTableData(entries);
        }

        private class ReminderDocument_ByHash : AbstractIndexCreationTask<RavenDbReminderDocument, ReminderDocument_ByHash.Result>
        {
            public class Result
            {
                public uint Hash { get; init; }
                public string GrainId { get; set; }
                public string ReminderName { get; set; }
                public DateTime StartAt { get; set; }
                public TimeSpan Period { get; set; }

            }

            public ReminderDocument_ByHash()
            {
                Map = documents => from reminderDocument in documents
                    let hash = reminderDocument.GrainId.GetHashCode()
                    select new Result
                    {
                        Hash = (uint)hash,
                        GrainId = reminderDocument.GrainId,
                        ReminderName = reminderDocument.ReminderName,
                        StartAt = reminderDocument.StartAt,
                        Period = reminderDocument.Period
                    };
            }
        }

        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            try
            {
                using var session = _documentStore.OpenAsyncSession();
                //var reminders = await session.Query<RavenDbReminderDocument>()
                //    .Where(r => r.GrainId.GetHashCode() >= begin && r.GrainId.GetHashCode() < end)
                //    .ToListAsync();

                var reminders = await session.Query<ReminderDocument_ByHash.Result, ReminderDocument_ByHash>()
                    .Where(doc => doc.Hash >= begin && doc.Hash < end)
                    .ToListAsync();

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
                Console.WriteLine(e);
                throw;
            }


        }

        public async Task<ReminderEntry> ReadRow(GrainId grainId, string reminderName)
        {
            using var session = _documentStore.OpenAsyncSession();
            var key = $"reminders/{grainId}/{reminderName}";

            var reminder = await session.LoadAsync<RavenDbReminderDocument>(key) ?? new RavenDbReminderDocument();
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
            var key = $"reminders/{entry.GrainId}/{entry.ReminderName}";

            var reminder = await session.LoadAsync<RavenDbReminderDocument>(key) ?? new RavenDbReminderDocument();
            reminder.GrainId = entry.GrainId.ToString();
            reminder.ReminderName = entry.ReminderName;
            reminder.StartAt = entry.StartAt;
            reminder.Period = entry.Period;
            reminder.LastUpdated = DateTime.UtcNow;
            //reminder.HashCode = (uint)reminder.GrainId.GetHashCode();

            await session.StoreAsync(reminder, changeVector: entry.ETag, id: key);
            await session.SaveChangesAsync();

            return session.Advanced.GetChangeVectorFor(reminder);
        }


        public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
        {
            using var session = _documentStore.OpenAsyncSession();
            var key = $"{grainId}_{reminderName}";

            var reminder = await session.LoadAsync<RavenDbReminderDocument>(key);
            if (reminder == null) 
                return false;

            if (session.Advanced.GetChangeVectorFor(reminder) != eTag) 
                throw new InconsistentStateException($"ETag mismatch for {key}");

            session.Delete(reminder);
            await session.SaveChangesAsync();
            return true;
        }

        public async Task TestOnlyClearTable()
        {
            using var session = _documentStore.OpenAsyncSession();
            var query = session.Query<RavenDbReminderDocument>();
            foreach (var entry in query) 
                session.Delete(entry);
            
            await session.SaveChangesAsync();
        }
    }
}
