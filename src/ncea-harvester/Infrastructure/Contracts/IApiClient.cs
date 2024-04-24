namespace Ncea.Harvester.Infrastructure.Contracts;

public interface IApiClient
{
    void CreateClient(string BaseUrl);
    Task<string> GetAsync(string apiUrl, CancellationToken cancellationToken);
    Task<string> PostAsync(string apiUrl, string requestData, CancellationToken cancellationToken);
}
