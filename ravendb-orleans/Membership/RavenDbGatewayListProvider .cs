using Orleans.Messaging;
using Orleans.Runtime;
using Raven.Client.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.RavenDB.Configuration;
using System.Net;
using SiloAddressClass = Orleans.Runtime.SiloAddress;


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

    public async Task InitializeGatewayListProvider()
    {
        _documentStore = new DocumentStore
        {
            Urls = _options.Urls,
            Database = _options.DatabaseName
        }.Initialize();

        _logger.LogInformation("Initializing RavenDB Gateway List Provider");
        await Task.CompletedTask;
    }

    public async Task<IList<Uri>> GetGateways()
    {
        using var session = _documentStore.OpenAsyncSession(_options.DatabaseName);
        var gateways = await session.Query<MembershipEntryDocument>()
            .Where(entry => entry.ClusterId == _options.ClusterId && entry.Status == SiloStatus.Active.ToString() && entry.ProxyPort > 0)
            .ToListAsync();

        var result = gateways.Select(ToGatewayUri).ToList();

        _logger.LogInformation("Retrieved {Count} active gateways from RavenDB", result.Count);
        return result;
    }

    private static Uri ToGatewayUri(MembershipEntryDocument membershipDocument)
    {
        var siloAddress = SiloAddressClass.FromParsableString(membershipDocument.SiloAddress);

        return SiloAddressClass.New(new IPEndPoint(siloAddress.Endpoint.Address, membershipDocument.ProxyPort), siloAddress.Generation).ToGatewayUri();
    }
}