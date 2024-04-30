using Ncea.Harvester.Infrastructure.Contracts;
using System.Text;

namespace Ncea.Harvester.Infrastructure;

public class ApiClient: IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private HttpClient _httpClient = new HttpClient();
    public ApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public void CreateClient(string BaseUrl)
    {
        _httpClient = _httpClientFactory.CreateClient(BaseUrl);
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<string> GetAsync(string apiUrl, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(apiUrl, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string>PostAsync(string apiUrl, string requestData, CancellationToken cancellationToken)
    {
        var content = new StringContent(requestData, Encoding.UTF8);
        var response = await _httpClient.PostAsync(apiUrl, content, cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
