namespace Orleans.Providers.RavenDb.Configuration
{
    /// <summary>
    /// Configuration options for RavenDB-based Orleans membership.
    /// </summary>
    public class RavenDbMembershipOptions : RavenDbOptions
    {
        /// <summary>
        /// The unique identifier of the Orleans cluster. This is used to scope membership data in RavenDB.
        /// </summary>
        public string? ClusterId { get; set; }

        /// <summary>
        /// Optional identifier used to support multiple Orleans ServiceIds within the same database.
        /// When set, this value will be included in document IDs and used to scope all membership data. 
        /// This allows multiple Orleans applications (clusters with different ServiceIds) to share a single database.
        /// If not set, the provider will assume a single-ServiceId-per-database model (default behavior).
        /// </summary>
        public string? ServiceId { get; set; }
    }
}
