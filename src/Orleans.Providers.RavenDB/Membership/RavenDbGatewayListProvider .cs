using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Messaging;
using Orleans.Providers.RavenDb.Configuration;
using Raven.Client.Documents;
using SiloAddressClass = Orleans.Runtime.SiloAddress;

namespace Orleans.Providers.RavenDb.Membership;

/// <summary>
/// Gateway list provider that retrieves active gateways from RavenDB for Orleans clients.
/// </summary>
public class RavenDbGatewayListProvider : IGatewayListProvider
{
    private readonly RavenDbMembershipOptions _options;
    private readonly ILogger _logger;
    private IDocumentStore? _documentStore;

    /// <summary>
    /// The maximum duration a gateway list can be stale before being refreshed.
    /// </summary>
    public TimeSpan MaxStaleness { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Indicates whether the gateway list is dynamic and can be updated at runtime.
    /// </summary>
    public bool IsUpdatable { get; } = true;

    public RavenDbGatewayListProvider(IOptions<RavenDbMembershipOptions> options, ILogger<RavenDbGatewayListProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task InitializeGatewayListProvider()
    {
        try
        {
            if (_options.DocumentStore != null)
            {
                _documentStore = _options.DocumentStore;
                _logger.LogInformation("Using externally provided DocumentStore.");
                return Task.CompletedTask;
            }

            _documentStore = new DocumentStore
            {
                Urls = _options.Urls,
                Database = _options.DatabaseName,
                Certificate = _options.Certificate,
                Conventions = _options.Conventions
            }.Initialize();

            _logger.LogInformation("Initializing RavenDB Gateway List Provider for ClusterId='{ClusterId}'", _options.ClusterId);

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize RavenDB Gateway List Provider.");
            throw;
        }

    }

    /// <summary>
    /// Retrieves a list of currently active gateway URIs from RavenDB.
    /// </summary>
    /// <returns>A list of URIs pointing to active Orleans gateways.</returns>
    public async Task<IList<Uri>> GetGateways()
    {
        _logger.LogDebug("Retrieving active gateways from RavenDB for ClusterId='{ClusterId}'", _options.ClusterId);

        try
        {
            using var session = _documentStore!.OpenAsyncSession(_options.DatabaseName);
            var gateways = await session.Query<MembershipEntryDocument, RavenDbMembershipTable.MembershipByClusterIdAliveTimeStatusAndPort>()
                .Where(entry => entry.ClusterId == _options.ClusterId && entry.Status == SiloStatus.Active.ToString() && entry.ProxyPort > 0)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} active gateways from RavenDB", gateways.Count);

            return gateways.Select(ToGatewayUri).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active gateways from RavenDB for ClusterId='{ClusterId}'", _options.ClusterId);
            throw;
        }

    }

    private static Uri ToGatewayUri(MembershipEntryDocument membershipDocument)
    {
        var siloAddress = SiloAddressClass.FromParsableString(membershipDocument.SiloAddress);
        var ep = new IPEndPoint(siloAddress.Endpoint.Address, membershipDocument.ProxyPort);

        return SiloAddressClass.New(ep, siloAddress.Generation).ToGatewayUri();
    }
}