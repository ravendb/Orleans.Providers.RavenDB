using Microsoft.Extensions.Logging;
using Orleans.Providers.RavenDb.Configuration;
using Orleans.Serialization;
using Orleans.Storage;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Orleans.Providers.RavenDb.StorageProviders
{
    /// <summary>
    /// Grain storage provider using RavenDB as the backend storage.
    /// Implements <see cref="IGrainStorage"/> and participates in the Orleans silo lifecycle.
    /// </summary>
    public class RavenDbGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly RavenDbGrainStorageOptions _options;
        private readonly ILogger<RavenDbGrainStorage> _logger;
        private IDocumentStore? _documentStore;
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


        /// <summary>
        /// Reads persisted grain state from RavenDB and populates the provided <paramref name="grainState"/>.
        /// </summary>
        /// <typeparam name="T">The type of the grain state.</typeparam>
        /// <param name="stateName">The logical name of the state.</param>
        /// <param name="grainId">The unique grain ID.</param>
        /// <param name="grainState">The grain state object to populate.</param>
        public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug("Reading state for stateName={StateName}, grainId={GrainId}", stateName, grainId);
            try
            {
                using var session = _documentStore!.OpenAsyncSession();

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

        /// <summary>
        /// Writes grain state to RavenDB, updating the stored document and its ETag.
        /// </summary>
        /// <typeparam name="T">The type of the grain state.</typeparam>
        /// <param name="stateName">The logical name of the state.</param>
        /// <param name="grainId">The unique grain ID.</param>
        /// <param name="grainState">The grain state to persist.</param>
        public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug("Writing state for stateName={StateName}, grainId={GrainId}", stateName, grainId);

            try
            {
                using var session = _documentStore!.OpenAsyncSession(new SessionOptions
                {
                    TransactionMode = _transactionMode,
                });

                string key = GetKey<T>(stateName, grainId);
                var entity = grainState.State;

                if (_transactionMode == TransactionMode.ClusterWide && grainState.ETag != null)
                {
                    entity = await session.LoadAsync<T>(key);

                    var inMemorySession = session as InMemoryDocumentSessionOperations;
                    using var blittableJson = inMemorySession!.JsonConverter.ToBlittable(grainState.State, documentInfo: null);
                    inMemorySession.JsonConverter.PopulateEntity(entity, key, blittableJson);
                }
                else
                {
                    await session.StoreAsync(grainState.State, changeVector: grainState.ETag, id: key);
                }

                await session.SaveChangesAsync();

                grainState.ETag = session.Advanced.GetChangeVectorFor(entity);
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

        /// <summary>
        /// Deletes grain state from RavenDB, clearing the document for the specified grain.
        /// </summary>
        /// <typeparam name="T">The type of the grain state.</typeparam>
        /// <param name="stateName">The logical name of the state.</param>
        /// <param name="grainId">The unique grain ID.</param>
        /// <param name="grainState">The grain state being cleared.</param>
        public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
        {
            _logger.LogDebug("Clearing state for stateName={StateName}, grainId={GrainId}", stateName, grainId);

            try
            {
                using var session = _documentStore!.OpenAsyncSession(new SessionOptions
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

        /// <summary>
        /// Registers the storage provider to participate in the Orleans silo lifecycle.
        /// </summary>
        /// <param name="observer">The silo lifecycle observer.</param>
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
                _logger.LogInformation("Initializing RavenDB document store for database '{DatabaseName}' at URLs: {Urls}", _options.DatabaseName, string.Join(", ", _options.Urls ?? []));

                _documentStore = new DocumentStore
                {
                    Database = _options.DatabaseName,
                    Certificate = _options.Certificate,
                    Conventions = _options.Conventions,
                    Urls = _options.Urls
                }.Initialize();

                if (_options.EnsureDatabaseExists)
                {
                    // Ensure the database exists
                    var dbExists = _documentStore.Maintenance.Server.Send(new GetDatabaseRecordOperation(_options.DatabaseName)) != null;
                    if (dbExists == false)
                        _documentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(_options.DatabaseName)));
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RavenDB document store.");
                throw new OrleansException($"Failed to initialize RavenDB document store. Exception={Environment.NewLine}{ex.Message}");
            }
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

        private string GetKey<T>(string grainType, GrainId grainId)
        {
            if (_options.KeyGenerator != null)
                return _options.KeyGenerator(grainType, grainId);

            var typeName = grainType != "state" ? grainType : typeof(T).Name;
            var separator = _options.GrainKeySeparator ?? "/";
            return $"{typeName}{separator}{grainId}";
        }
    }
}
