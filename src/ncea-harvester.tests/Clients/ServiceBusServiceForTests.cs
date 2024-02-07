using Moq;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Ncea.Harvester.Infrastructure;
using Ncea.Harvester.Models;

namespace Ncea.Harvester.Tests.Clients;

public static class ServiceBusServiceForTests
{
    public static ServiceBusService Get(out Mock<ServiceBusSender> mockServiceBusSender)
    {
        var appSettings = Options.Create(new HarvesterConfigurations() { ServiceBusConnectionString = "azure.servicebus-connection", ServiceBusQueueName = "test-queue" });
        var mockServiceBusClient = new Mock<ServiceBusClient>();
        mockServiceBusSender = new Mock<ServiceBusSender>();
        var service = new ServiceBusService(appSettings, mockServiceBusClient.Object);

        // Set up the mock to return the mock sender
        mockServiceBusClient.Setup(x => x.CreateSender(It.IsAny<string>())).Returns(mockServiceBusSender.Object);

        return service;
    }
}
