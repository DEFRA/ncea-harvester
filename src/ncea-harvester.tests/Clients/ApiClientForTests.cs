using Azure.Security.KeyVault.Secrets;
using Azure;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq.Protected;
using System.Net;
using ncea.harvester.infra;
using System.Net.Http;

namespace ncea_harvester.tests.Clients
{
    public static class ApiClientForTests
    {
        
        public static ApiClient Get(HttpResponseMessage responseMessage)
        {
            MockRepository _mockRepository;
            Mock<HttpMessageHandler> _handlerMock;
            Mock<IHttpClientFactory> _httpClientFactoryMock;
            HttpClient _httpClient;

            _mockRepository = new(MockBehavior.Default);
            _handlerMock = _mockRepository.Create<HttpMessageHandler>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClient = new(_handlerMock.Object);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            _httpClientFactoryMock
                .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

            var apiClient = new ApiClient(_httpClientFactoryMock.Object);
            apiClient.CreateClient("https://baseUri");

            return apiClient;
        }
    }
}
