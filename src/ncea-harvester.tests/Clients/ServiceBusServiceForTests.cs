using Moq;
using Azure.Messaging.ServiceBus;
using Ncea.Harvester.Infrastructure;
using Ncea.Harvester.Models;
using Ncea.Harvester.Enums;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

namespace Ncea.Harvester.Tests.Clients;

public static class ServiceBusServiceForTests
{
    public static ServiceBusService Get(out Mock<ServiceBusSender> mockServiceBusSender)
    {
        var configuration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        //configuration.Setup(c => c.GetValue<string>(It.IsAny<string>())).Returns("test");
        configuration.Setup(c => c.GetSection(It.IsAny<String>())).Returns(new Mock<IConfigurationSection>().Object);
        configuration.SetupGet(x => x[It.IsAny<string>()]).Returns("the string you want to return");
        var harvesterConfiguration = new HarvesterConfiguration() { ProcessorType = It.IsAny<ProcessorType>() };
        
        var mockServiceBusClient = new Mock<ServiceBusClient>();
        mockServiceBusSender = new Mock<ServiceBusSender>();
        mockServiceBusClient.Setup(x => x.CreateSender(It.IsAny<string>())).Returns(mockServiceBusSender.Object);

        var azureClientFactory = new Mock<IAzureClientFactory<ServiceBusSender>>();
        azureClientFactory.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(mockServiceBusSender.Object);

        var service = new ServiceBusService(configuration.Object, azureClientFactory.Object);        

        return service;
    }

    public static ServiceBusService GetServiceBusWithError(out Mock<ServiceBusSender> mockServiceBusSender)
    {
        var configuration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        //configuration.Setup(c => c.GetValue<string>(It.IsAny<string>())).Returns("test");
        configuration.Setup(c => c.GetSection(It.IsAny<String>())).Returns(new Mock<IConfigurationSection>().Object);
        configuration.SetupGet(x => x[It.IsAny<string>()]).Returns("the string you want to return");
        var harvesterConfiguration = new HarvesterConfiguration() { ProcessorType = It.IsAny<ProcessorType>() };
        
        var mockServiceBusClient = new Mock<ServiceBusClient>();
        mockServiceBusSender = new Mock<ServiceBusSender>();
        mockServiceBusClient.Setup(x => x.CreateSender(It.IsAny<string>())).Returns(mockServiceBusSender.Object);


        var azureClientFactory = new Mock<IAzureClientFactory<ServiceBusSender>>();
        azureClientFactory.Setup(c => c.CreateClient(It.IsAny<string>())).Returns(mockServiceBusSender.Object);

        var service = new ServiceBusService(configuration.Object, azureClientFactory.Object);
        mockServiceBusSender.Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>())).Throws<ServiceBusException>();       

        return service;
    }
}
