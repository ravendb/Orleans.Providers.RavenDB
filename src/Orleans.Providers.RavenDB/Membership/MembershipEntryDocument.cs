namespace Orleans.Providers.RavenDb.Membership
{
    /// <summary>
    /// Represents a membership entry document stored in RavenDB for Orleans clustering.
    /// </summary>
    public sealed class MembershipEntryDocument
    {
        public string ClusterId { get; set; }
        public string SiloName { get; set; }
        public string SiloAddress { get; set; }
        public string HostName { get; set; }
        public string Status { get; set; }
        public int ProxyPort { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime IAmAliveTime { get; set; }
        public string RoleName { get; set; }
        public List<SuspectTime> SuspectTimes { get; set; }

    }

    public sealed class TableVersionDocument
    {
        public string DeploymentId { get; set; }

        public int Version { get; set; } // The global version of the membership table

    }

    public sealed class SuspectTime
    {
        public string Address { get; set; }
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
