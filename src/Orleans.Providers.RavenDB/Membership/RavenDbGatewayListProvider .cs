using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Messaging;
using Orleans.Providers.RavenDb.Configuration;
using Raven.Client.Documents;
using SiloAddressClass = Orleans.Runtime.SiloAddress;


namespace Orleans.Providers.RavenDb.Membership;

public class RavenDbGatewayListProvider : IGatewayListProvider
{
    private readonly RavenDbMembershipOptions _options;
    private readonly ILogger _logger;
    private IDocumentStore _documentStore;

    public TimeSpan MaxStaleness { get; } = TimeSpan.FromSeconds(30); // Default staleness duration

    public bool IsUpdatable { get; } = true; // Gateways can update dynamically

    public RavenDbGatewayListProvider(IOptions<RavenDbMembershipOptions> options, ILogger logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task InitializeGatewayListProvider()
    {
        _documentStore = new DocumentStore
        {
            Urls = _options.Urls,
            Database = _options.DatabaseName
        }.Initialize();

        _logger.LogInformation("Initializing RavenDB Gateway List Provider");

        return Task.CompletedTask;
    }

    public async Task<IList<Uri>> GetGateways()
    {
        using var session = _documentStore.OpenAsyncSession(_options.DatabaseName);
        var gateways = await session.Query<MembershipEntryDocument>()
            .Where(entry => entry.ClusterId == _options.ClusterId && entry.Status == SiloStatus.Active.ToString() && entry.ProxyPort > 0)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} active gateways from RavenDB", gateways.Count);

        return gateways.Select(ToGatewayUri).ToList();
    }

    private static Uri ToGatewayUri(MembershipEntryDocument membershipDocument)
    {
        var siloAddress = SiloAddressClass.FromParsableString(membershipDocument.SiloAddress);
        var ep = new IPEndPoint(siloAddress.Endpoint.Address, membershipDocument.ProxyPort);

        return SiloAddressClass.New(ep, siloAddress.Generation).ToGatewayUri();
    }
}