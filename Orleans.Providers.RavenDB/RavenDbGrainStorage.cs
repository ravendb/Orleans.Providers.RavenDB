using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDB.Configuration;
using Orleans.Runtime;
using Orleans.Storage;
using Raven.Client.Documents;

namespace Orleans.Providers.RavenDB
{
    public class RavenDbGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly RavenDbGrainStorageOptions _options;
        private readonly ILogger<RavenDbGrainStorage> _logger;

        private IDocumentStore _documentStore;

        public RavenDbGrainStorage(RavenDbGrainStorageOptions options, ILogger<RavenDbGrainStorage> logger)
        {
            _options = options;
            _logger = logger;
        }

        public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return DoAndLog(nameof(ReadStateAsync), () =>
                ReadAsync(stateName, grainId, grainState), grainId);
        }

        public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return DoAndLog(nameof(WriteStateAsync), () =>
                WriteAsync(stateName, grainId, grainState), grainId);
        }

        public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            return DoAndLog(nameof(ClearStateAsync), () => 
                ClearAsync(stateName, grainId, grainState), grainId);
        }

        public async Task ReadAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            using var session = _documentStore.OpenAsyncSession();
            string key = GetKey<T>(stateName, grainId);

            var storedData = await session.LoadAsync<T>(key);
            if (storedData != null)
            {
                grainState.RecordExists = true;
                var changeVector = session.Advanced.GetChangeVectorFor(storedData);
                grainState.ETag = changeVector;

                grainState.State = storedData;
            }

            /*if (storedData != null)
            {
                grainState.RecordExists = true;

                if (existing.Contains(FieldDoc))
                {
                    grainState.ETag = existing[FieldEtag].AsString;

                    grainState.State = serializer.Deserialize<T>(existing[FieldDoc]);
                }
                else
                {
                    existing.Remove(FieldId);

                    grainState.State = serializer.Deserialize<T>(existing);
                }
            }*/

        }

        public async Task WriteAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            using var session = _documentStore.OpenAsyncSession();
            string key = GetKey<T>(stateName, grainId);
            var etag = grainState.ETag;

            await session.StoreAsync(grainState.State, changeVector: etag, id: key);
            await session.SaveChangesAsync();
        }

        public async Task ClearAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            using var session = _documentStore.OpenAsyncSession();
            string key = GetKey<T>(stateName, grainId);
            session.Delete(key);

            await session.SaveChangesAsync();
        }

        public void Participate(ISiloLifecycle observer)
        {
            observer.Subscribe<RavenDbGrainStorage>(ServiceLifecycleStage.ApplicationServices, Init);
        }

        private Task Init(CancellationToken ct)
        {
            _documentStore = new DocumentStore
            {
                Database = _options.DatabaseName,
                Certificate = _options.Certificate,
                Conventions = _options.Conventions,
                Urls = _options.Urls
            }.Initialize();

            return Task.CompletedTask;
        }

        private Task DoAndLog(string actionName, Func<Task> action, GrainId grainId)
        {
            return DoAndLog(actionName, async () => { await action(); return true; }, grainId);
        }

        private async Task<T> DoAndLog<T>(string actionName, Func<Task<T>> action, GrainId grainId)
        {
            _logger.LogDebug($"{nameof(RavenDbGrainStorage)}.{actionName} was called on grain {grainId}");

            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger.LogError(1, ex, $"{nameof(RavenDbGrainStorage)}.{actionName} failed. grainId = {grainId}. Exception={ex.Message}");

                throw;
            }
        }

        private static string GetKey<T>(string grainType, GrainId grainId)
        {
            var t = grainType != "state" ? grainType : typeof(T).Name;
            return $"{t}/{grainId}";
        }
    }
}
