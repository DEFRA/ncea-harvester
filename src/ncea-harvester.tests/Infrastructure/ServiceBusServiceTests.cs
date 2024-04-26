using Azure.Messaging.ServiceBus;
using Moq;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Tests.Clients;

namespace Ncea.Harvester.Tests.Infrastructure;

public class ServiceBusServiceTests
{
    [Fact]
    public async Task SendMessage_ShouldSendMessageToQueue()
    {
        // Arrange
        var service = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender>  mockServiceBusSender);
        
        var sendMessageRequest = new SendMessageRequest("test-datasource-name", "tets-file-id", "test-message");
        // Act
        await service.SendMessageAsync(sendMessageRequest, It.IsAny<CancellationToken>());

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
    }
}
