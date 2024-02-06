﻿using System.Text;

namespace ncea.harvester.infra
{
    public interface IApiClient
    {
        void CreateClient(string BaseUrl);
        Task<string> GetAsync(string apiUrl);
        Task<string> PostAsync(string apiUrl, string requestData);
    }

    public class ApiClient: IApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient _httpClient;
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
            var response = await _httpClient.GetAsync(apiUrl);

            response.EnsureSuccessStatusCode();
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
}