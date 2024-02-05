using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Moq;
using ncea.harvester.infra;
using ncea.harvester.Models;
using ncea_harvester.tests.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
