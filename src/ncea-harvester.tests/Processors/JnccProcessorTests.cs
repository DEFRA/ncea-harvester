using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Moq;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors;
using Ncea.Harvester.Tests.Clients;
using System.Net;

namespace Ncea.Harvester.Tests.Processors;

public class JnccProcessorTests
{
    [Fact]
    public async Task Process_ShouldSendMessagesToServiceBus() {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var expectedData = "<html><body><a href=\"a.xml\">a</a><a href=\"b.xml\">b</a></body></html>";
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        IOptions<HarvesterConfigurations> appSettings = Options.Create(new HarvesterConfigurations() { ServiceBusConnectionString = "azure.servicebus-connection", ServiceBusQueueName = "test-queue", KeyVaultUri = "keyVault-uri", Processor = new Processor() { DataSourceApiBase="https://base-uri", DataSourceApiUrl="/test-url", ProcessorType= Ncea.Harvester.Constants.ProcessorType.Jncc, Type=""} });

        // Act
        var jnccService = new JnccProcessor(apiClient, serviceBusService, appSettings);
        await jnccService.Process();            

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Exactly(2));
    }

    [Fact]
    public async Task Process_ShouldNotSendMessagesToServiceBus()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var expectedData = "<html><body></body></html>";
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        IOptions<HarvesterConfigurations> appSettings = Options.Create(new HarvesterConfigurations() { ServiceBusConnectionString = "azure.servicebus-connection", ServiceBusQueueName = "test-queue", KeyVaultUri = "keyVault-uri", Processor = new Processor() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = Ncea.Harvester.Constants.ProcessorType.Jncc, Type = "" } });

        // Act
        var jnccService = new JnccProcessor(apiClient, serviceBusService, appSettings);
        await jnccService.Process();

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Never);
    }

    [Fact]
    public async Task Process_ShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        IOptions<HarvesterConfigurations> appSettings = Options.Create(new HarvesterConfigurations() { ServiceBusConnectionString = "azure.servicebus-connection", ServiceBusQueueName = "test-queue", KeyVaultUri = "keyVault-uri", Processor = new Processor() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = Ncea.Harvester.Constants.ProcessorType.Jncc, Type = "" } });

        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, serviceBusService, appSettings);
        await Assert.ThrowsAsync<HttpRequestException>(() => jnccService.Process());
    }
}
