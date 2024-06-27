using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Moq;
using ncea.harvester.Services;
using Ncea.Harvester.BusinessExceptions;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors;
using Ncea.Harvester.Services;
using Ncea.Harvester.Services.Contracts;
using Ncea.Harvester.Tests.Clients;
using Ncea.Mapper.Services;
using Ncea.Mapper.Services.Contracts;
using ncea_harvester.tests.Clients;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace Ncea.Harvester.Tests.Processors;

public class MedinProcessorTests
{
    private readonly Mock<ILogger<MedinProcessor>> _mockLogger;
    private readonly Mock<ILogger<OrchestrationService>> _mockOrchestrationServiceLogger;
    private readonly Mock<IBackUpService> _backUpServiceMock;
    private readonly Mock<IDeletionService> _deletionServiceMock;
    private readonly Mock<IValidationService> _validationServiceMock;

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

        _backUpServiceMock = new Mock<IBackUpService>();
        _backUpServiceMock.Setup(x => x.BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
        _deletionServiceMock = new Mock<IDeletionService>();
        _deletionServiceMock.Setup(x => x.DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
        _validationServiceMock = new Mock<IValidationService>();
        _validationServiceMock.Setup(x => x.IsValid(It.IsAny<XElement>())).Returns(true);
    }

    [Fact]
    public async Task Process_WhenValidMedinMetadataIsHarvested_ShouldSendMessagesToServiceBus()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        string expectedData = GetFileContent("MEDIN_Metadata_srv_valid.xml");
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationServiceMock.Object, _mockLogger.Object, harvesterConfiguration);
        await medinService.ProcessAsync(It.IsAny<CancellationToken>());

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Once);
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Once);
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Process_WhenMedinMetadataWithNoFileIdentifierIsHarvested_ShouldLogErrorMessage()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        string expectedData = GetFileContent("MEDIN_Metadata_dataset_no_fileidentifier.xml");
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        //var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        var configuration = ConfigurationForTests.GetConfiguration();
        var harvesterConfig = ConfigurationForTests.GetHarvesterConfiguration(ProcessorType.Medin);
        var xmlNodeService = new XmlNodeService(configuration);
        var _validationService = new ValidationService(harvesterConfig!, xmlNodeService);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfig);
        await medinService.ProcessAsync(It.IsAny<CancellationToken>());

        //Assert
        _mockLogger.Verify(
            m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            It.IsAny<string>()
        );
    }    

    [Fact]
    public async Task Process_WhenMedinMetadataWithNoTitleIsHarvested_ShouldLogErrorMessage()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        string expectedData = GetFileContent("MEDIN_Metadata_dataset_no_title.xml");
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        var configuration = ConfigurationForTests.GetConfiguration();
        var harvesterConfig = ConfigurationForTests.GetHarvesterConfiguration(ProcessorType.Medin);
        var xmlNodeService = new XmlNodeService(configuration);
        var _validationService = new ValidationService(harvesterConfig!, xmlNodeService);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfig);
        await medinService.ProcessAsync(It.IsAny<CancellationToken>());

        //Assert
        _mockLogger.Verify(
            m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            It.IsAny<string>()
        );
    }

    [Fact]
    public async Task Process_WhenMedinMetadataWithNoAbstractIsHarvested_ShouldLogErrorMessage()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        string expectedData = GetFileContent("MEDIN_Metadata_dataset_no_abstract.xml");
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        var configuration = ConfigurationForTests.GetConfiguration();
        var harvesterConfig = ConfigurationForTests.GetHarvesterConfiguration(ProcessorType.Medin);
        var xmlNodeService = new XmlNodeService(configuration);
        var _validationService = new ValidationService(harvesterConfig!, xmlNodeService);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfig);
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
    public async Task Process_WhenMedinMetadataWithNoPointOfContactIsHarvested_ShouldLogErrorMessage()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        string expectedData = GetFileContent("MEDIN_Metadata_dataset_no_pointofcontact.xml");
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(expectedData),
        };
        var apiClient = ApiClientForTests.Get(httpResponse);
        var harvesterConfiguration = new HarvesterConfiguration() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);
        var configuration = ConfigurationForTests.GetConfiguration();
        var harvesterConfig = ConfigurationForTests.GetHarvesterConfiguration(ProcessorType.Medin);
        var xmlNodeService = new XmlNodeService(configuration);
        var _validationService = new ValidationService(harvesterConfig!, xmlNodeService);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationService, _mockLogger.Object, harvesterConfig);
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
                                                      out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                                      out Mock<BlobClient> mockBlobClient);
        var loggerMock = new Mock<ILogger<MedinProcessor>>();
        var orchestrationservice = new OrchestrationService(blobServiceMock, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationServiceMock.Object, loggerMock.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act & Assert
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationServiceMock.Object, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);


        // Act & Assert
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationServiceMock.Object, _mockLogger.Object, harvesterConfiguration);
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
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);


        // Act & Assert
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationServiceMock.Object, _mockLogger.Object, harvesterConfiguration);
        await Assert.ThrowsAsync<DataSourceConnectionException>(() => medinService.ProcessAsync(It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Process_WhenValidMedinMetadataIsHarvestedAndOneBatchFails_ShouldSendMessagesToServiceBus()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        string batch1ExpectedData = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                            "<csw:GetRecordsResponse xmlns:csw=\"http://www.opengis.net/cat/csw/2.0.2\">" +
                            "  <csw:SearchStatus timestamp=\"2024-02-15T17:54:36.664Z\" />" +
                            "  <csw:SearchResults numberOfRecordsMatched=\"300\" numberOfRecordsReturned=\"2\" elementSet=\"full\" nextRecord=\"101\">" +
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

        string batch3ExpectedData = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                            "<csw:GetRecordsResponse xmlns:csw=\"http://www.opengis.net/cat/csw/2.0.2\">" +
                            "  <csw:SearchStatus timestamp=\"2024-02-15T17:54:36.664Z\" />" +
                            "  <csw:SearchResults numberOfRecordsMatched=\"300\" numberOfRecordsReturned=\"2\" elementSet=\"full\" nextRecord=\"301\">" +
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
        var httpResponse1 = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(batch1ExpectedData),
        };

        var httpResponse3 = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(batch3ExpectedData),
        };
        
        var apiClient = ApiClientForTests.GetWithBatchError(true, httpResponse1, null, httpResponse3);
        var harvesterConfiguration = new HarvesterConfiguration() 
        { 
            DataSourceApiBase = "https://base-uri", 
            DataSourceApiUrl = "/test-url/gmd?maxRecords={{maxRecords}}&startPosition={{startPosition}}", 
            ProcessorType = ProcessorType.Medin, Type = "" 
        };
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var orchestrationservice = new OrchestrationService(blobService, serviceBusService, _mockOrchestrationServiceLogger.Object);

        // Act
        var medinService = new MedinProcessor(apiClient, orchestrationservice, _backUpServiceMock.Object, _deletionServiceMock.Object, _validationServiceMock.Object, _mockLogger.Object, harvesterConfiguration);
        await medinService.ProcessAsync(It.IsAny<CancellationToken>());

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Exactly(4));
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(4));
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Exactly(4));
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    private static string GetFileContent(string fileName)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", fileName);
        var xDoc = new XmlDocument();
        xDoc.Load(filePath);
        var messageBody = xDoc.InnerXml;

        return messageBody;
    }
}
