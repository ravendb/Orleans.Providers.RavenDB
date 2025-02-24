using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Storage;
using Raven.Client.Documents;

namespace Orleans.Providers.RavenDb.StorageProviders
{
    /// <summary>
    /// Grain storage provider using RavenDB as the backend storage.
    /// </summary>
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
            _logger.LogDebug("Reading state for stateName={StateName}, grainId={GrainId}", stateName, grainId);
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
                _logger.LogError(ex, "Error reading state for stateName={StateName}, grainId={GrainId}", stateName, grainId);
                throw new OrleansException($"Failed to read state for grain {grainId}. Exception={Environment.NewLine}{ex.Message}");
            }
        }
        public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug("Writing state for stateName={StateName}, grainId={GrainId}", stateName, grainId);

            try
            {
                using var session = _documentStore.OpenAsyncSession();
                string key = GetKey<T>(stateName, grainId);
                var etag = grainState.ETag;

                await session.StoreAsync(grainState.State, changeVector: etag, id: key);
                await session.SaveChangesAsync();

                grainState.ETag = session.Advanced.GetChangeVectorFor(grainState.State);
                grainState.RecordExists = true;
            }
            catch (Raven.Client.Exceptions.ConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency exception writing state for stateName={StateName}, grainId={GrainId}", stateName, grainId);
                throw new OrleansException($"Concurrency exception writing state for grain {grainId}. Exception={Environment.NewLine}{ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing state for stateName={StateName}, grainId={GrainId}", stateName, grainId);
                throw new OrleansException($"Failed to write state for grain {grainId}. Exception={Environment.NewLine}{ex.Message}");
            }
        }

        public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug("Clearing state for stateName={StateName}, grainId={GrainId}", stateName, grainId);

            try
            {
                using var session = _documentStore.OpenAsyncSession();
                string key = GetKey<T>(stateName, grainId);

                session.Delete(key);
                await session.SaveChangesAsync();

                grainState.RecordExists = false;
                grainState.ETag = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing state for stateName={StateName}, grainId={GrainId}", stateName, grainId);
                throw new OrleansException($"Failed to clear state for grain {grainId}. Exception={Environment.NewLine}{ex.Message}");
            }
        }

        public void Participate(ISiloLifecycle observer)
        {
            observer.Subscribe<RavenDbGrainStorage>(ServiceLifecycleStage.ApplicationServices, Init);
        }

        /// <summary>
        /// Initializes the RavenDB document store.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        private Task Init(CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Initializing RavenDB document store for database '{DatabaseName}' at URLs: {Urls}", _options.DatabaseName, string.Join(", ", _options.Urls));

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
                _logger.LogError(ex, "Error initializing RavenDB document store.");
                throw new OrleansException($"Failed to initialize RavenDB document store. Exception={Environment.NewLine}{ex.Message}");
            }
        }

        internal static string GetKey<T>(string grainType, GrainId grainId)
        {
            var typeName = grainType != "state" ? grainType : typeof(T).Name;
            return $"{typeName}/{grainId}";
        }
    }
}
