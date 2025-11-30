namespace Platform.Web;
public class GrainApiClient(HttpClient httpClient)
{
    public async Task CallIncrementAsync(Guid grainId, CancellationToken cancellationToken = default)
    {
        var result = await httpClient.PostAsync($"/counter/{grainId}", null, cancellationToken: cancellationToken);
    }
}