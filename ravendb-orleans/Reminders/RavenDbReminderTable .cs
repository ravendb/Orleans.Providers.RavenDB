using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Runtime;
using Orleans.Storage;
using Raven.Client.Documents;

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

        public async Task<ReminderTableData> ReadRows(uint begin, uint end)
        {
            using var session = _documentStore.OpenAsyncSession();
            var reminders = await session.Query<RavenDbReminderDocument>()
                                         .Where(r => r.GrainId.GetHashCode() >= begin && GrainHashToRange(r.GrainId) < end)
                                         .ToListAsync();

            var entries = reminders.Select(r => new ReminderEntry
            {
                GrainId =  GrainId.Parse(r.GrainId),
                ReminderName =  r.ReminderName,
                StartAt =  r.StartAt,
                Period = r.Period,
                ETag =  session.Advanced.GetChangeVectorFor(r)
            }).ToList();
            
            return new ReminderTableData(entries);
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
            reminder.HashCode = (uint)reminder.GrainId.GetHashCode();

            await session.StoreAsync(reminder, entry.ETag);
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

        // Additional helper to hash and assign ranges.
        private static uint GrainHashToRange(string grainId) => (uint)grainId.GetHashCode();
    }
}
