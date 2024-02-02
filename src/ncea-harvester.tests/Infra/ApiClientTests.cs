using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ncea.harvester.infra;
using Xunit;

namespace ncea_harvester.tests.Infra
{
    public class ApiClientTests
    {
        private MockRepository _mockRepository;
        private Mock<HttpMessageHandler> _handlerMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private HttpClient _httpClient;

        private void Init()
        {
            _mockRepository = new(MockBehavior.Default);
            _handlerMock = _mockRepository.Create<HttpMessageHandler>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpClient = new(_handlerMock.Object);
        }

        [Fact]
        public async Task GetDataAsync_Should_ReturnData_When_HttpRequestIsSuccessful()
        {
            Init();

            // Arrange
            var expectedData = "Mocked API response";

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedData),
                });

            _httpClientFactoryMock
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);



            // Act
            var apiClient = new ApiClient(_httpClientFactoryMock.Object);
            apiClient.CreateClient("https://baseUri");
            var result = await apiClient.GetAsync("/apiurl");

            // Assert
            Assert.Equal(expectedData, result);
        }

        [Fact]
        public async Task GetDataAsync_Should_ThrowException_When_HttpRequestFails()
        {
            Init();

            // Arrange
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            _httpClientFactoryMock
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);


            var apiClient = new ApiClient(_httpClientFactoryMock.Object);
            apiClient.CreateClient("https://baseUri");

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => apiClient.GetAsync(It.IsAny<string>()));
        }
    }

}
