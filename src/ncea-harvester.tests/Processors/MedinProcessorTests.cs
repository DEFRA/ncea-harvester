using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Ncea.Harvester.Constants;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors;
using Ncea.Harvester.Tests.Clients;
using System.Net;

namespace Ncea.Harvester.Tests.Processors;

public class MedinProcessorTests
{
    [Fact]
    public async Task Process_ShouldSendMessagesToServiceBus()
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
        var appSettings = Options.Create(new HarvesterConfigurations() { Processor = new Processor() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" } });
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var logger = new Logger<MedinProcessor>(new LoggerFactory());
        // Act
        var medinService = new MedinProcessor(apiClient, serviceBusService, blobService, logger, appSettings);
        await medinService.Process();

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Exactly(2));
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(2));
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Exactly(2));
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Process_ShouldNotSendMessagesToServiceBus()
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
        var appSettings = Options.Create(new HarvesterConfigurations() { Processor = new Processor() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" } });
        var blobServiceMock = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                                      out Mock<BlobContainerClient> mockBlobContainerClient,
                                                      out Mock<BlobClient> mockBlobClient);
        var loggerMock = new Mock<ILogger<MedinProcessor>>();

        // Act
        var medinService = new MedinProcessor(apiClient, serviceBusService, blobServiceMock, loggerMock.Object, appSettings);
        await medinService.Process();

        // Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Never);
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Never);
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Never);
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
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
        var appSettings = Options.Create(new HarvesterConfigurations() { Processor = new Processor() { DataSourceApiBase = "https://base-uri", DataSourceApiUrl = "/test-url", ProcessorType = ProcessorType.Medin, Type = "" } });
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<BlobClient> mockBlobClient);
        var logger = new Logger<MedinProcessor>(new LoggerFactory());


        // Act & Assert
        var medinService = new MedinProcessor(apiClient, serviceBusService, blobService,logger, appSettings);
        await Assert.ThrowsAsync<HttpRequestException>(() => medinService.Process());        
    }
}
