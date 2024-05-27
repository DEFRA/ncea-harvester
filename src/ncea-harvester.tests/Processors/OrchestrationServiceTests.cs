using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Moq;
using Ncea.Harvester.Services;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Models;
using Ncea.Harvester.Tests.Clients;
using Ncea.Harvester.Infrastructure.Contracts;

namespace Ncea.Harvester.Tests.Processors;

public class OrchestrationServiceTests
{

    [Fact]
    public async Task SaveHarvestedXmlFile_WhenHarvetsedItemWithEmptyFileIdentifiers_ThenBlobClientIsNotCalled()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var logger = new Logger<OrchestrationService>(new LoggerFactory());

        var orchestrationService = new OrchestrationService(blobService, serviceBusService, logger);

        //Act
        await orchestrationService.SaveHarvestedXmlFile(It.IsAny<string>(), "test-file-id", "test-xml-content", It.IsAny<CancellationToken>());

        //Assert
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(1));
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Exactly(1));
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task SaveHarvestedXmlFile_WhenExceptionThrownFromBlobClientWhileSavingHarvestedFile_ThenBlobClientIsNotCalled()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var blobService = BlobServiceForTests.GetWithError(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var mockLogger = new Mock<ILogger<OrchestrationService>>(MockBehavior.Strict);
        mockLogger.Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            )
        );

        var orchestrationService = new OrchestrationService(blobService, serviceBusService, mockLogger.Object);

        //Act
        await orchestrationService.SaveHarvestedXmlFile(It.IsAny<string>(), "test-file-id", "test-xml-content", It.IsAny<CancellationToken>());

        //Assert
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(1));
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Exactly(1));
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

        mockLogger.Verify(
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
    public async Task SendMessagesToHarvestedQueue_WhenHarvetsedItemsListIsEmpty_ThenServiceBusClientIsNotCalled()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var logger = new Logger<OrchestrationService>(new LoggerFactory());

        var orchestrationService = new OrchestrationService(blobService, serviceBusService, logger);
        var harvetsedItemsList = new List<HarvestedFile>();


        //Act
        await orchestrationService.SendMessagesToHarvestedQueue(It.IsAny<DataSource>(), harvetsedItemsList, It.IsAny<CancellationToken>());

        //Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Never);
    }

    [Fact]
    public async Task SendMessagesToHarvestedQueue_WhenHarvetsedItemsListConatinsItemsWithEmptyBlobUrls_ThenBlobClientIsNotCalled()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.Get(out Mock<ServiceBusSender> mockServiceBusSender);
        var blobService = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var logger = new Logger<OrchestrationService>(new LoggerFactory());

        var orchestrationService = new OrchestrationService(blobService, serviceBusService, logger);
        var harvetsedItemsList = new List<HarvestedFile>
        {
            new HarvestedFile(string.Empty, string.Empty, string.Empty, null),
            new HarvestedFile("test-file-id", "test-blob-url", "test-file-content", null),
            new HarvestedFile("", string.Empty, string.Empty, null),
            new HarvestedFile(" ", string.Empty, string.Empty, null)
        };


        //Act
        await orchestrationService.SendMessagesToHarvestedQueue(It.IsAny<DataSource>(), harvetsedItemsList, It.IsAny<CancellationToken>());

        //Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Exactly(1));
    }

    [Fact]
    public async Task SendMessagesToHarvestedQueue_WhenExceptionThrownFromBlobClientWhileSavingHarvestedFile_ThenBlobClientIsNotCalled()
    {
        //Arrange
        var serviceBusService = ServiceBusServiceForTests.GetServiceBusWithError(out Mock<ServiceBusSender> mockServiceBusSender);
        var blobService = BlobServiceForTests.GetWithError(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);
        var mockLogger = new Mock<ILogger<OrchestrationService>>(MockBehavior.Strict);
        mockLogger.Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            )
        );
        mockLogger.Setup(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            )
        );

        var orchestrationService = new OrchestrationService(blobService, serviceBusService, mockLogger.Object);
        var harvetsedItemsList = new List<HarvestedFile>
        {
            new HarvestedFile(string.Empty, string.Empty, string.Empty, null),
            new HarvestedFile("test-file-id", "test-blob-url", "test-file-content", null),
            new HarvestedFile("", string.Empty, string.Empty, null),
            new HarvestedFile(" ", string.Empty, string.Empty, null)
        };


        //Act
        await orchestrationService.SendMessagesToHarvestedQueue(It.IsAny<DataSource>(), harvetsedItemsList, It.IsAny<CancellationToken>());

        //Assert
        mockServiceBusSender.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Exactly(1));

        mockLogger.Verify(
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
}
