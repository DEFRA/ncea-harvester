using System.Net;
using Moq;
using Ncea.Harvester.Tests.Clients;

namespace Ncea.Harvester.Tests.Infrastructure;

public class ApiClientTests
{
    [Fact]
    public async Task GetAsync_Should_ReturnData_When_HttpRequestIsSuccessful()
    {
        // Arrange
        GetSuccessResponse(out string expectedData, out HttpResponseMessage response);
        var apiClient = ApiClientForTests.Get(response);

        
        // Act
        var result = await apiClient.GetAsync("/apiurl", It.IsAny<CancellationToken>());

        // Assert
        Assert.Equal(expectedData, result);
    }

    [Fact]
    public async Task GetAsync_Should_ThrowException_When_HttpRequestFails()
    {
        // Arrange
        var response = new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.InternalServerError
                            };
        var apiClient = ApiClientForTests.Get(response);


        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => apiClient.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task PostAsync_Should_ReturnData_When_HttpRequestIsSuccessful()
    {
        // Arrange
        GetSuccessResponse(out string expectedData, out HttpResponseMessage response);
        var apiClient = ApiClientForTests.Get(response);


        // Act
        var result = await apiClient.PostAsync("/apiurl", "test-request", It.IsAny<CancellationToken>());

        // Assert
        Assert.Equal(expectedData, result);
    }

    [Fact]
    public async Task PostAsync_Should_ThrowException_When_HttpRequestFails()
    {
        // Arrange
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };
        var apiClient = ApiClientForTests.Get(response);


        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => apiClient.PostAsync(It.IsAny<string>(), "test-request", It.IsAny<CancellationToken>()));
    }

    private static void GetSuccessResponse(out string expectedData, out HttpResponseMessage response)
    {
        expectedData = "Mocked API response";
        response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
    }
}
