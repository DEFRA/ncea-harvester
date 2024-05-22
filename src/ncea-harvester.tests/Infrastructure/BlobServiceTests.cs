using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Moq;
using Ncea.harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Tests.Clients;

namespace Ncea.Harvester.Tests.Infrastructure;

public class BlobServiceTests
{
    [Fact]
    public async Task SaveAsync_ShouldCallRequiredBlobServiceMethods()
    {
        // Arrange
        var service = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        // Act
        await service.SaveAsync(new SaveBlobRequest(Stream.Null, "file1.xml", "jncc"), CancellationToken.None);
        await service.SaveAsync(new SaveBlobRequest(Stream.Null, "file2.xml", "jncc"), CancellationToken.None);
        await service.SaveAsync(new SaveBlobRequest(Stream.Null, "file3.xml", "jncc"), CancellationToken.None);

        // Assert
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(3));
        mockBlobContainerClient.Verify(x => x.GetBlobClient(It.IsAny<string>()), Times.Exactly(3));
        mockBlobClient.Verify(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(),It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task BackUpContainerAsync_WhenBlobsFromPreviousRunExists_BackUpTheBlobs()
    {
        // Arrange
        var service = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        // Act
        await service.BackUpContainerAsync(new BackUpContainerRequest(It.IsAny<string>(), It.IsAny<string>()), It.IsAny<CancellationToken>());

        // Assert
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(2));
        mockBlobContainerClient.Verify(x => x.GetBlobsAsync(BlobTraits.None, BlobStates.None, "", It.IsAny<CancellationToken>()), Times.Exactly(1));
        mockBlobBatchClient.Verify(x => x.CreateBatch(), Times.Exactly(1));
        mockBlobBatchClient.Verify(x => x.SubmitBatchAsync(It.IsAny<BlobBatch>(), true, It.IsAny<CancellationToken>()), Times.Exactly(1));
    }

    [Fact]
    public async Task DeleteBlobsAsync_WhenBlobsExistsInBackupContainer_DeleteTheBlobs()
    {
        // Arrange
        var service = BlobServiceForTests.Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                              out Mock<BlobContainerClient> mockBlobContainerClient,
                                              out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                              out Mock<BlobClient> mockBlobClient);

        // Act
        await service.DeleteBlobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>());

        // Assert
        mockBlobServiceClient.Verify(x => x.GetBlobContainerClient(It.IsAny<string>()), Times.Exactly(1));
        mockBlobContainerClient.Verify(x => x.GetBlobsAsync(BlobTraits.None, BlobStates.None, "", It.IsAny<CancellationToken>()), Times.Exactly(1));
        mockBlobBatchClient.Verify(x => x.CreateBatch(), Times.Exactly(1));
        mockBlobBatchClient.Verify(x => x.SubmitBatchAsync(It.IsAny<BlobBatch>(), true, It.IsAny<CancellationToken>()), Times.Exactly(1));
    }
}
