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

public class MedinProcessorTests
{
    private readonly Mock<ILogger<MedinProcessor>> _mockLogger;
    private readonly Mock<ILogger<OrchestrationService>> _mockOrchestrationServiceLogger;

    public MedinProcessorTests()
    {
        _mockOrchestrationServiceLogger = new Mock<ILogger<OrchestrationService>>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<MedinProcessor>>(MockBehavior.Strict);
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
    public async Task Process_WhenValidMedinMetadataIsHarvested_ShouldSendMessagesToServiceBus()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        string expectedData = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                            "<csw:GetRecordsResponse xmlns:csw=\"http://www.opengis.net/cat/csw/2.0.2\">" +
                            "  <csw:SearchStatus timestamp=\"2024-02-15T17:54:36.664Z\" />" +
                            "  <csw:SearchResults numberOfRecordsMatched=\"2\" numberOfRecordsReturned=\"2\" elementSet=\"full\" nextRecord=\"3\">" +
                            "    <gmd:MD_Metadata xmlns:gmd=\"http://www.isotc211.org/2005/gmd\" xmlns:gco=\"http://www.isotc211.org/2005/gco\">" +
                            "      <gmd:fileIdentifier>" +
                            "        <gco:CharacterString>abce1a60-c7f2-42fd-81e9-03d54ab01f0f</gco:CharacterString>" +
                            "      </gmd:fileIdentifier>" +
                            "    </gmd:MD_Metadata>" +
                            "    <gmd:MD_Metadata xmlns:gmd=\"http://www.isotc211.org/2005/gmd\" xmlns:gco=\"http://www.isotc211.org/2005/gco\">" +
                            "      <gmd:fileIdentifier>" +
                            "        <gco:CharacterString>defe1a60-c7f2-42fd-81e9-03d54ab01f0f</gco:CharacterString>" +
                            "      </gmd:fileIdentifier>" +
                            "    </gmd:MD_Metadata>" +
                            "  </csw:SearchResults>" +
                            "</csw:GetRecordsResponse>";
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        
            // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await medinService.ProcessAsync(It.IsAny<CancellationToken>());

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Exactly(2));
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(2));
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Exactly(2));
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Process_WhenMedinMetadataWithNoFileIdentifierIsHarvested_ShouldLogErrorMessage()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        string expectedData = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                            "<csw:GetRecordsResponse xmlns:csw=\"http://www.opengis.net/cat/csw/2.0.2\">" +
                            "  <csw:SearchStatus timestamp=\"2024-02-15T17:54:36.664Z\" />" +
                            "  <csw:SearchResults numberOfRecordsMatched=\"2\" numberOfRecordsReturned=\"2\" elementSet=\"full\" nextRecord=\"3\">" +
                            "    <gmd:MD_Metadata xmlns:gmd=\"http://www.isotc211.org/2005/gmd\" xmlns:gco=\"http://www.isotc211.org/2005/gco\">" +
                            "    </gmd:MD_Metadata>" +
                            "  </csw:SearchResults>" +
                            "</csw:GetRecordsResponse>";
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await medinService.ProcessAsync(It.IsAny<CancellationToken>());

        //Assert
        _mockLogger.Verify(
            m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(1),
            It.IsAny<string>()
        );
    }

    [Fact]
    public async Task Process_WhenErrorMedinMetadataXmlIsHarvested_ShouldNotSendMessagesToServiceBus()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var expectedData = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                           "<ows:ExceptionReport xmlns:ows=\"http://www.opengis.net/ows\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" version=\"1.2.0\" xsi:schemaLocation=\"http://www.opengis.net/ows http://schemas.opengis.net/ows/1.0.0/owsExceptionReport.xsd\">" +
                           "  <ows:Exception exceptionCode=\"NoApplicableCode\">" +
                           "    <ows:ExceptionText>java.lang.RuntimeException: org.fao.geonet.csw.common.exceptions.InvalidParameterValueEx: code=InvalidParameterValue, locator=startPosition, message=Start position (17804) can't be greater than number of matching records (17803 for current search).</ows:ExceptionText>" +
                           "  </ows:Exception>" +
                           "</ows:ExceptionReport>";
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };
        var blobServiceMock = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                                      out Mock<BlobContainerClient> mockBlobContainerClient,
                                                      out Mock<BlobClient> mockBlobClient);
        var loggerMock = new Mock<ILogger<MedinProcessor>>();
        var orchestrationservice = new OrchestrationService(blobServiceMock, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, loggerMock.Object, harvesterConfiguration);
        await medinService.ProcessAsync(It.IsAny<CancellationToken>());

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Never);
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Never);
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Never);
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Process_WhenMedinDatasourceApiCallFailedWithHttpException_MedinServiceRequestShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };

        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act & Assert
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => medinService.ProcessAsync(It.IsAny<CancellationToken>()));        
    }

    [Fact]
    public async Task Process_WhenMedinDatasourceApiCallFailedWhenTokenExpiredWithTaskCancellationException_MedinServiceRequestShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var apiClient = ApiClientForTests.GetWithError(true);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };

        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);


        // Act & Assert
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => medinService.ProcessAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Process_WhenMedinDatasourceApiCallFailedWhenTokenNotExpiredWithTaskCancellationException_MedinServiceRequestShouldThrowError()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var apiClient = ApiClientForTests.GetWithError(false);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };

        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);


        // Act & Assert
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => medinService.ProcessAsync(It.IsAny<CancellationToken>()));
    }
}
