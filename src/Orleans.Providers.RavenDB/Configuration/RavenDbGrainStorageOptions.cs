namespace Orleans.Providers.RavenDb.Configuration
{
    /// <summary>
    /// Configuration options for RavenDB-based grain storage.
    /// </summary>
    public class RavenDbGrainStorageOptions : RavenDbOptions
    {
        /// <summary>
        /// An optional delegate used to generate a key for storing a grain.
        /// If not provided, a default key generation strategy will be used.
        /// </summary>
        public GrainKeyGenerator? KeyGenerator { get; set; } = null;

        /// <summary>
        /// Indicates whether cluster-wide transactions should be used when persisting grain state.
        /// </summary>
        public bool UseClusterWideTransactions { get; set; } = false;

        /// <summary>
        /// An optional separator to use when generating grain keys.
        /// If not provided, the default separator ('/') will be used.
        /// </summary>
        public string? GrainKeySeparator { get; set; } = null;

    }

    /// <summary>
    /// A delegate used to generate a RavenDB document key for a given grain.
    /// </summary>
    /// <param name="grainType">The full type name of the grain.</param>
    /// <param name="grainId">The unique identifier of the grain.</param>
    /// <returns>A string representing the document key.</returns>
    public delegate string GrainKeyGenerator(string grainType, GrainId grainId);

}
