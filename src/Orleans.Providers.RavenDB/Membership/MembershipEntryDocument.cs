namespace Orleans.Providers.RavenDb.Membership
{
    /// <summary>
    /// Represents a membership entry document stored in RavenDB for Orleans clustering.
    /// Each entry corresponds to a silo participating in the cluster.
    /// </summary>
    public sealed class MembershipEntryDocument
    {
        /// <summary>
        /// The unique identifier of the cluster this membership entry belongs to.
        /// </summary>
        public string ClusterId { get; set; }

        /// <summary>
        /// The unique name of the silo.
        /// </summary>
        public string SiloName { get; set; }

        /// <summary>
        /// The silo's network address in string form.
        /// </summary>
        public string SiloAddress { get; set; }

        /// <summary>
        /// The hostname of the machine running the silo.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// The current status of the silo (e.g., Active, Dead, etc.).
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// The proxy port used by the silo.
        /// </summary>
        public int ProxyPort { get; set; }

        /// <summary>
        /// The timestamp when the silo started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The last timestamp when the silo reported it was alive.
        /// </summary>
        public DateTime IAmAliveTime { get; set; }

        /// <summary>
        /// The role name assigned to the silo.
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// A list of suspect times, representing silos that have been suspected of failure.
        /// </summary>
        public List<SuspectTime> SuspectTimes { get; set; }

    }

    /// <summary>
    /// Represents the version document of the Orleans membership table.
    /// Used for concurrency control.
    /// </summary>
    public sealed class TableVersionDocument
    {
        /// <summary>
        /// The deployment or cluster identifier this version document belongs to.
        /// </summary>
        public string DeploymentId { get; set; }

        /// <summary>
        /// The current global version of the membership table.
        /// </summary>
        public int Version { get; set; }

    }


    /// <summary>
    /// Represents a suspect time entry, indicating when a specific silo was suspected of failure.
    /// </summary>
    public sealed class SuspectTime
    {
        /// <summary>
        /// The address of the suspected silo, serialized as a string.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The time when the suspicion was recorded, serialized as a string.
        /// </summary>
        public string IAmAliveTime { get; set; }

        public static SuspectTime Create(Tuple<SiloAddress, DateTime> tuple)
        {
            return new SuspectTime { Address = tuple.Item1.ToParsableString(), IAmAliveTime = LogFormatter.PrintDate(tuple.Item2) };
        }

        public Tuple<SiloAddress, DateTime> ToTuple()
        {
            return Tuple.Create(SiloAddress.FromParsableString(Address), LogFormatter.ParseDate(IAmAliveTime));
        }
    }

}
