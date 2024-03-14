﻿using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Processors;
using System.Text;

namespace Ncea.Harvester.Infrastructure;

public class ApiClient: IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private HttpClient _httpClient = new HttpClient();
    private readonly ILogger<ApiClient> _logger;
    public ApiClient(IHttpClientFactory httpClientFactory, ILogger<ApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public void CreateClient(string BaseUrl)
    {
        _httpClient = _httpClientFactory.CreateClient(BaseUrl);
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<string> GetAsync(string apiUrl)
    {
        var response = await _httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Api call failed");
        }

        _logger.LogDebug("Status Code:" + response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string>PostAsync(string apiUrl, string requestData)
    {
        var content = new StringContent(requestData, Encoding.UTF8);
        var response = await _httpClient.PostAsync(apiUrl, content);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
