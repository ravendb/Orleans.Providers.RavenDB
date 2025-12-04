using System.Security.Cryptography.X509Certificates;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;

namespace Orleans.Providers.RavenDb.Configuration
{
    /// <summary>
    /// Base configuration options for connecting to a RavenDB database.
    /// </summary>
    public class RavenDbOptions
    {
        /// <summary>
        /// The name of the RavenDB database to connect to.
        /// </summary>
        public string? DatabaseName { get; set; }

        /// <summary>
        /// The client certificate used to authenticate with the RavenDB server.
        /// </summary>
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// The set of conventions used by the RavenDB client for serialization and document behavior.
        /// </summary>
        public DocumentConventions? Conventions { get; set; }

        /// <summary>
        /// An array of URLs pointing to the RavenDB server nodes.
        /// </summary>
        public string[]? Urls { get; set; }

        /// <summary>
        /// Whether to wait for index updates after saving changes to ensure query consistency.
        /// </summary>
        public bool WaitForIndexesAfterSaveChanges { get; set; }

        /// <summary>
        /// Determines whether the provider should automatically create the database
        /// if it does not already exist. 
        /// </summary>
        public bool EnsureDatabaseExists { get; set; } = true;

        /// <summary>
        /// Allows injecting a custom RavenDB document store. If set, the internal creation will be skipped.
        /// </summary>
        public IDocumentStore? DocumentStore { get; set; }
    }
}
