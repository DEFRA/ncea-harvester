using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Moq;
using Ncea.Harvester.Services;
using Ncea.Harvester.Services.Contracts;
using Ncea.Harvester.BusinessExceptions;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors;
using Ncea.Harvester.Tests.Clients;
using System.Net;
using ncea.harvester.Services;
using Ncea.Mapper.Services;
using ncea_harvester.tests.Clients;
using System.Xml;
using Castle.Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Ncea.Harvester.Tests.Processors;

public class JnccProcessorTests
{
    private readonly Mock<ILogger<JnccProcessor>> _mockLogger;
    private readonly Mock<ILogger<OrchestrationService>> _mockOrchestrationServiceLogger;
    private readonly Mock<IBackUpService> _backUpServiceMock;
    private readonly Mock<IDeletionService> _deletionServiceMock;
    private readonly IConfigurationRoot _configuration;
    private readonly HarvesterConfiguration _harvesterConfig;
    private readonly XmlNodeService _xmlNodeService;
    private readonly ValidationService _validationService;

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

        _backUpServiceMock = new Mock<IBackUpService>();
        _deletionServiceMock = new Mock<IDeletionService>();

        _backUpServiceMock = new Mock<IBackUpService>();
        _backUpServiceMock.Setup(x => x.BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
        _deletionServiceMock = new Mock<IDeletionService>();
        _deletionServiceMock.Setup(x => x.DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));


        //Variables
        _configuration = ConfigurationForTests.GetConfiguration();
        _harvesterConfig = ConfigurationForTests.GetHarvesterConfiguration(ProcessorType.Jncc);
        _xmlNodeService = new XmlNodeService(_configuration);
        _validationService = new ValidationService(_harvesterConfig!, _xmlNodeService);
    }

    [Fact]
    public async Task Process_WhenValidMetadataIsHarvested_ShouldSendMessagesToServiceBus() {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var expectedData = "<html><body><a href=\"a.xml\">a</a></body></html>";
        var metaDataXmlStr = GetFileContent("JNCC_Metadata.xml");
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase="https://base-uri", DataSourceApiUrl="/test-url", ProcessorType= ProcessorType.Jncc, Type=""};
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var jnccMockService = new Mock<JnccProcessor>(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
        jnccMockService.Setup(x => x.GetJnccMetadata(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(metaDataXmlStr));
        await jnccMockService.Object.ProcessAsync(It.IsAny<CancellationToken>());

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Once);
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Once);
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
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
                                                      out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                                      out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobServiceMock, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
        // Act & Assert
        var jnccService = new JnccProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => jnccService.GetJnccMetadata(It.IsNotNull<string>(), It.IsNotNull<string>(), It.IsNotNull<CancellationToken>()));
    }



    private string GetFileContent(string fileName)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", fileName);
        var xDoc = new XmlDocument();
        xDoc.Load(filePath);
        var messageBody = xDoc.InnerXml;

        return messageBody;
    }
}
