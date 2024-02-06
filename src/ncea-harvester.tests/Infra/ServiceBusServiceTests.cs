using Azure.Messaging.ServiceBus;
using Moq;
using ncea_harvester.tests.Clients;

namespace ncea_harvester.tests.Infra
{
    public class ServiceBusServiceTests
    {
        [Fact]
        public async Task SendMessage_ShouldSendMessageToQueue()
        {
            // Arrange
            var service = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender>  mockServiceBusSender);
            
            // Act
            await service.SendMessageAsync("Hello, World!");

            // Assert
            mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        }

    }
}
