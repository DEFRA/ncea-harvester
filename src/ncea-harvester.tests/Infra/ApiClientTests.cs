using System.Net;
using Moq;
using ncea_harvester.tests.Clients;

namespace ncea_harvester.tests.Infra
{
    public class ApiClientTests
    {
        [Fact]
        public async Task GetDataAsync_Should_ReturnData_When_HttpRequestIsSuccessful()
        {
            // Arrange
            var expectedData = "Mocked API response";
            var response = new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = new StringContent(expectedData),
                            };

            var apiClient = ApiClientForTests.Get(response);

            
            // Act
            var result = await apiClient.GetAsync("/apiurl");

            // Assert
            Assert.Equal(expectedData, result);
        }

        [Fact]
        public async Task GetDataAsync_Should_ThrowException_When_HttpRequestFails()
        {
            // Arrange
            var response = new HttpResponseMessage
                                {
                                    StatusCode = HttpStatusCode.InternalServerError
                                };
            var apiClient = ApiClientForTests.Get(response);


            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => apiClient.GetAsync(It.IsAny<string>()));
        }
    }

}
