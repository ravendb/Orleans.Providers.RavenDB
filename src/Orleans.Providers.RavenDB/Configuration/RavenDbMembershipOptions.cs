﻿namespace Orleans.Providers.RavenDb.Configuration
{
    /// <summary>
    /// Configuration options for RavenDB-based Orleans membership.
    /// </summary>
    public class RavenDbMembershipOptions : RavenDbOptions
    {
        /// <summary>
        /// The unique identifier of the Orleans cluster. This is used to scope membership data in RavenDB.
        /// </summary>
        public string ClusterId { get; set; }
    }
}
