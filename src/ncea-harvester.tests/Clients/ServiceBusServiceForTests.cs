using Moq;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ncea.harvester.infra;
using ncea.harvester.Models;

namespace ncea_harvester.tests.Clients
{
    public static class ServiceBusServiceForTests
    {
        public static ServiceBusService Get(out Mock<ServiceBusSender> mockServiceBusSender)
        {
            var appSettings = Options.Create(new AppSettings() { ServiceBusConnectionString = "azure.servicebus-connection", ServiceBusQueueName = "test-queue" });
            var mockServiceBusClient = new Mock<ServiceBusClient>();
            mockServiceBusSender = new Mock<ServiceBusSender>();
            var service = new ServiceBusService(appSettings, mockServiceBusClient.Object);

            // Set up the mock to return the mock sender
            mockServiceBusClient.Setup(x => x.CreateSender(It.IsAny<string>())).Returns(mockServiceBusSender.Object);

            return service;
        }
    }
}
