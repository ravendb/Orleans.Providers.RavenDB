using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Serialization;
using Orleans.Storage;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Raven.Client.Json.Serialization.NewtonsoftJson;

namespace Orleans.Providers.RavenDb.StorageProviders
{
    /// <summary>
    /// Grain storage provider using RavenDB as the backend storage.
    /// </summary>
    public class RavenDbGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly RavenDbGrainStorageOptions _options;
        private readonly ILogger<RavenDbGrainStorage> _logger;
        private IDocumentStore _documentStore;
        private readonly TransactionMode _transactionMode;

        public RavenDbGrainStorage(IServiceProvider services, RavenDbGrainStorageOptions options, ILogger<RavenDbGrainStorage> logger)
        {
            _options = options;
            _logger = logger;
            _transactionMode = _options.UseClusterWideTransactions
                ? TransactionMode.ClusterWide
                : TransactionMode.SingleNode;

            AddOrleansJsonConvertors(services);
        }

        private void AddOrleansJsonConvertors(IServiceProvider services)
        {
            var settings = OrleansJsonSerializerSettings.GetDefaultSerializerSettings(services);

            _options.Conventions ??= new DocumentConventions
            {
                Serialization = new NewtonsoftJsonSerializationConventions()
            };

            ((NewtonsoftJsonSerializationConventions)_options.Conventions.Serialization).CustomizeJsonSerializer +=
                serializer =>
                {
                    foreach (var converter in settings.Converters)
                    {
                        serializer.Converters.Add(converter);
                    }
                };
        }

        public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug("Reading state for stateName={StateName}, grainId={GrainId}", stateName, grainId);
            try
            {
                using var session = _documentStore.OpenAsyncSession(new SessionOptions
                {
                    TransactionMode = _transactionMode
                });

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
                using var session = _documentStore.OpenAsyncSession(new SessionOptions
                {
                    TransactionMode = _transactionMode
                });

                string key = GetKey<T>(stateName, grainId);

                if (_transactionMode == TransactionMode.SingleNode)
                {
                    await session.StoreAsync(grainState.State, changeVector: grainState.ETag, id: key);
                }
                else
                {
                    // no optimistic concurrency if we're using a cluster tx 
                    await session.StoreAsync(grainState.State, id: key);
                }

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
                using var session = _documentStore.OpenAsyncSession(new SessionOptions
                {
                    TransactionMode = _transactionMode
                });

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

        private string GetKey<T>(string grainType, GrainId grainId)
        {
            if (_options.KeyGenerator != null)
                return _options.KeyGenerator(grainType, grainId);

            var typeName = grainType != "state" ? grainType : typeof(T).Name;
            return $"{typeName}/{grainId}";
        }
    }
}
