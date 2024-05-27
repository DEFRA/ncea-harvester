using Moq;
using Moq.Protected;
using Ncea.Harvester.Infrastructure;

namespace Ncea.Harvester.Tests.Clients;

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

    public static ApiClient GetWithError(bool IsCancellationRequested)
    {
        MockRepository _mockRepository;
        Mock<HttpMessageHandler> _handlerMock;
        Mock<IHttpClientFactory> _httpClientFactoryMock;
        HttpClient _httpClient;

        _mockRepository = new(MockBehavior.Default);
        _handlerMock = _mockRepository.Create<HttpMessageHandler>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClient = new(_handlerMock.Object);

        var tokenSource = new CancellationTokenSource();
        
        if(IsCancellationRequested)
          tokenSource.Cancel();
        
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException(null, null, tokenSource.Token));

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
        .Returns(_httpClient);

        var apiClient = new ApiClient(_httpClientFactoryMock.Object);
        apiClient.CreateClient("https://baseUri");

        return apiClient;
    }
    public static ApiClient GetWithBatchError(bool IsCancellationRequested, 
        HttpResponseMessage batch1Response,
        HttpResponseMessage? batch2Response,
        HttpResponseMessage batch3Response)
    {
        MockRepository _mockRepository;
        Mock<HttpMessageHandler> _handlerMock;
        Mock<IHttpClientFactory> _httpClientFactoryMock;
        HttpClient _httpClient;

        _mockRepository = new(MockBehavior.Default);
        _handlerMock = _mockRepository.Create<HttpMessageHandler>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClient = new(_handlerMock.Object);

        var tokenSource = new CancellationTokenSource();

        if (IsCancellationRequested)
            tokenSource.Cancel();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.Query == "?maxRecords=1&startPosition=1"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(batch1Response);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.Query == "?maxRecords=100&startPosition=1"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(batch1Response);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.Query == "?maxRecords=100&startPosition=101"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException(null, null, tokenSource.Token));

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
                ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.Query == "?maxRecords=100&startPosition=201"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(batch3Response);

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
        .Returns(_httpClient);

        var apiClient = new ApiClient(_httpClientFactoryMock.Object);
        apiClient.CreateClient("https://baseUri");

        return apiClient;
    }
}
