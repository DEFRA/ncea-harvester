using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Moq;
using ncea.harvester.Services;
using Ncea.Harvester.BusinessExceptions;
using Ncea.Harvester.Constants;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors;
using Ncea.Harvester.Tests.Clients;
using System.Net;

namespace Ncea.Harvester.Tests.Processors;

public class JnccProcessorTests
{
    private readonly Mock<ILogger<JnccProcessor>> _mockLogger;
    private readonly Mock<ILogger<OrchestrationService>> _mockOrchestrationServiceLogger;    

    public JnccProcessorTests()
    {
        _mockOrchestrationServiceLogger = new Mock<ILogger<OrchestrationService>>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<JnccProcessor>>(MockBehavior.Strict);
        _mockLogger.Setup(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            )
        );
        _mockLogger.Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            )
        );
    }

    [Fact]
    public async Task Process_WhenValidMetadataIsHarvested_ShouldSendMessagesToServiceBus() {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var expectedData = "<html><body><a href=\"a.xml\">a</a><a href=\"b.xml\">b</a></body></html>";
        var metaDataXmlStr = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n\r\n<gmd:MD_Metadata\r\n        xmlns:gmd=\"http://www.isotc211.org/2005/gmd\"\r\n        xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"\r\n        xmlns:gml=\"http://www.opengis.net/gml\" xmlns:gts=\"http://www.isotc211.org/2005/gts\"\r\n        xmlns:mdc=\"https://github.com/DEFRA/ncea-geonetwork/tree/main/core-geonetwork/schemas/iso19139.mdc/src/main/plugin/iso19139.mdc/schema/mdc\"\r\n        xmlns:gco=\"http://www.isotc211.org/2005/gco\">\r\n  <gmd:fileIdentifier>\r\n    <gco:CharacterString>8b1fd363-cfed-49f0-b6e2-8eab3138a735</gco:CharacterString>\r\n  </gmd:fileIdentifier></gmd:MD_Metadata>";
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase="https://base-uri", DataSourceApiUrl="/test-url", ProcessorType= ProcessorType.Jncc, Type=""};
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var jnccMockService = new Mock<JnccProcessor>(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        jnccMockService.Setup(x => x.GetJnccMetadata(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(metaDataXmlStr));
        await jnccMockService.Object.ProcessAsync(It.IsAny<CancellationToken>());

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Exactly(2));
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(2));
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Exactly(2));
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Process_WhenMetadataIsNotAvailable_ShouldNotSendMessagesToServiceBus()
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
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Jncc, Type = "" };
        var blobServiceMock = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                                      out Mock<BlobContainerClient> mockBlobContainerClient,
                                                      out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobServiceMock, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await jnccService.ProcessAsync(It.IsAny<CancellationToken>());

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Never);
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Never);
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Never);
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenJnccDataSourceApiCallThrowsHttpException_ShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Jncc, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => jnccService.ProcessAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Process_WhenJnccDatasourceApiCallFailedWhenTokenExpiredWithTaskCancellationException_ShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var apiClient = ApiClientForTests.GetWithError(true);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Jncc, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => jnccService.ProcessAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Process_WhenJnccDatasourceApiCallFailedWhenTokenNotExpiredWithTaskCancellationException_ShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var apiClient = ApiClientForTests.GetWithError(false);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Jncc, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => jnccService.ProcessAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Process_WhenJnccMetadataWithNoFileIdentifierIsHarvested_ShouldLogMessage()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.GetServiceBusWithError(out Mock<ServiceBusSender> mockServiceBusSender);
        var expectedData = "<html><body><a href=\"a.xml\">a</a><a href=\"b.xml\">b</a></body></html>";
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Jncc, Type = "" };

        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await jnccService.ProcessAsync(It.IsAny<CancellationToken>());

        // Assert
        _mockLogger.Verify(
            m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2),
            It.IsAny<string>()
        );
    }    

    [Fact]
    public async Task Process_WhenJnccMetaDataApiCallThrowsHttpException_ShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Jncc, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => jnccService.GetJnccMetadata(It.IsNotNull<string>(), It.IsNotNull<string>(), It.IsNotNull<CancellationToken>()));
    }

    [Fact]
    public async Task Process_WhenJnccMetaDataApiCallFailedWhenTokenExpiredWithTaskCancellationException_ShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var apiClient = ApiClientForTests.GetWithError(true);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Jncc, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => jnccService.GetJnccMetadata(It.IsNotNull<string>(), It.IsNotNull<string>(), It.IsNotNull<CancellationToken>()));
    }

    [Fact]
    public async Task Process_WhenJnccMetaDataApiCallFailedWhenTokenNotExpiredWithTaskCancellationException_ShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var apiClient = ApiClientForTests.GetWithError(false);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Jncc, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => jnccService.GetJnccMetadata(It.IsNotNull<string>(), It.IsNotNull<string>(), It.IsNotNull<CancellationToken>()));
    }

}
