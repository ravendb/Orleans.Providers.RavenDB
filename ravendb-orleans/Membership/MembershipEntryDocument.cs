using Orleans.Runtime;

namespace Orleans.Providers.RavenDB.Membership
{
    public sealed class MembershipEntryDocument
    {
        public string ClusterId { get; set; }  // Same as DeploymentId
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
        public string DeploymentId { get; set; }  // Same as ClusterId

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
