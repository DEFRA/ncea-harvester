﻿using Ncea.Harvester.Infrastructure.Contracts;
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
        _httpClient = _httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<string> GetAsync(string apiUrl)
    {
        Console.WriteLine("Inside ApiClient GetAsync");
        Console.WriteLine("Host URL: " + _httpClient.BaseAddress);
        var response = await _httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Api call failed");
        }

        Console.WriteLine("Status Code:" + response.StatusCode);
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
