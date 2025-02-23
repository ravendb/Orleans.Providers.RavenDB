using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Storage;
using Raven.Client.Documents;

namespace Orleans.Providers.RavenDb.StorageProviders
{
    public class RavenDbGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly RavenDbOptions _options;
        private readonly ILogger<RavenDbGrainStorage> _logger;
        private IDocumentStore _documentStore;

        public RavenDbGrainStorage(RavenDbOptions options, ILogger<RavenDbGrainStorage> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug($"{nameof(RavenDbGrainStorage)}.{nameof(ReadStateAsync)} was called on grain {grainId}");

            try
            {
                using var session = _documentStore.OpenAsyncSession();
                string key = GetKey<T>(stateName, grainId);

                var storedData = await session.LoadAsync<T>(key);
                if (storedData != null)
                {
                    grainState.RecordExists = true;
                    grainState.ETag = session.Advanced.GetChangeVectorFor(storedData);
                    grainState.State = storedData;
                }
                else
                {
                    grainState.RecordExists = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(RavenDbGrainStorage)}.{nameof(ReadStateAsync)} failed for grainId={grainId}. Exception={ex.Message}");
                throw;
            }
        }

        public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug($"{nameof(RavenDbGrainStorage)}.{nameof(WriteStateAsync)} was called on grain {grainId}");

            try
            {
                using var session = _documentStore.OpenAsyncSession();
                string key = GetKey<T>(stateName, grainId);
                var etag = grainState.ETag;
                await session.StoreAsync(grainState.State, changeVector: etag, id: key);

                try
                {
                    await session.SaveChangesAsync();
                }
                catch (Raven.Client.Exceptions.ConcurrencyException ex)
                {
                    throw new OrleansException($"Optimistic concurrency violation, transaction aborted. Error message:{Environment.NewLine}{ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(RavenDbGrainStorage)}.{nameof(WriteStateAsync)} failed for grainId={grainId}. Exception={ex.Message}");
                throw;
            }
        }

        public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug($"{nameof(RavenDbGrainStorage)}.{nameof(ClearStateAsync)} was called on grain {grainId}");

            try
            {
                using var session = _documentStore.OpenAsyncSession();
                string key = GetKey<T>(stateName, grainId);
                session.Delete(key);

                await session.SaveChangesAsync();
                grainState.RecordExists = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(RavenDbGrainStorage)}.{nameof(ClearStateAsync)} failed for grainId={grainId}. Exception={ex.Message}");
                throw;
            }
        }

        public void Participate(ISiloLifecycle observer)
        {
            observer.Subscribe<RavenDbGrainStorage>(ServiceLifecycleStage.ApplicationServices, Init);
        }

        private Task Init(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Initializing RavenDbGrainStorage...");

                _documentStore = new DocumentStore
                {
                    Database = _options.DatabaseName,
                    Certificate = _options.Certificate,
                    Conventions = _options.Conventions,
                    Urls = _options.Urls
                }.Initialize();

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initializing RavenDbGrainStorage. Exception={ex.Message}");
                throw;
            }
        }

        internal static string GetKey<T>(string grainType, GrainId grainId)
        {
            var t = grainType != "state" ? grainType : typeof(T).Name;
            return $"{t}/{grainId}";
        }
    }
}
