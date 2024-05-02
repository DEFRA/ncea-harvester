using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Moq;
using ncea.harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure;

namespace Ncea.Harvester.Tests.Clients;

public static class BlobServiceForTests
{
    private static List<BlobItem> BlobItems = new List<BlobItem>();
    public static BlobService Get(out Mock<BlobServiceClient> mockBlobServiceClient,
                                  out Mock<BlobContainerClient> mockBlobContainerClient,
                                  out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                  out Mock<BlobClient> mockBlobClient)
    {
        mockBlobClient = new Mock<BlobClient>();
        mockBlobClient.Setup(x => x.Uri).Returns(new Uri(new Uri("https://base-uri-blob-storage"), "relative-uri-blob-storage"));
        mockBlobClient.Setup(x =>
            x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>())).Returns(Task.FromResult(AddBlobItems()));

        mockBlobServiceClient = new Mock<BlobServiceClient>();
        mockBlobContainerClient = new Mock<BlobContainerClient>();

        var blobContainerInfo = BlobsModelFactory.BlobContainerInfo(It.IsAny<ETag>(), It.IsAny<DateTimeOffset>());
        var mockContainerResponse = Response.FromValue(blobContainerInfo, new Mock<Response>().Object);
        
        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient(It.IsAny<string>())).Returns(mockBlobContainerClient.Object);
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        mockBlobContainerClient.Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue<bool>(true, Mock.Of<Response>()));
        mockBlobContainerClient.Setup<Task<Response<BlobContainerInfo>>>(x =>
            x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(),
            It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse);
        mockBlobContainerClient.Setup(x =>
            x.DeleteBlobIfExistsAsync(It.IsAny<string>(), It.IsAny<DeleteSnapshotsOption>(),
            It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue<bool>(true, new Mock<Response>().Object));



        var blobList = new BlobItem[]
        {
            BlobsModelFactory.BlobItem("Blob1"),
            BlobsModelFactory.BlobItem("Blob2"),
            BlobsModelFactory.BlobItem("Blob3")
        };
        Page<BlobItem> page = Page<BlobItem>.FromValues(blobList, null, Mock.Of<Response>());
        AsyncPageable<BlobItem> pageableBlobList = AsyncPageable<BlobItem>.FromPages(new[] { page });
        mockBlobContainerClient
            .Setup(m => m.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(pageableBlobList);

        mockBlobBatchClient = new Mock<IBlobBatchClientWrapper>();
        mockBlobBatchClient.Setup(m => m.CreateBatch())
            .Returns(new Mock<BlobBatch>().Object);

        mockBlobBatchClient.Setup(m => m.SubmitBatchAsync(It.IsAny<BlobBatch>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new BlobService(mockBlobServiceClient.Object, mockBlobBatchClient.Object);

        return service;
    }


    public static BlobService GetWithError(out Mock<BlobServiceClient> mockBlobServiceClient,
                                  out Mock<BlobContainerClient> mockBlobContainerClient,
                                  out Mock<IBlobBatchClientWrapper> mockBlobBatchClient,
                                  out Mock<BlobClient> mockBlobClient)
    {
        mockBlobServiceClient = new Mock<BlobServiceClient>();
        mockBlobContainerClient = new Mock<BlobContainerClient>();
        mockBlobClient = new Mock<BlobClient>();
        mockBlobClient.Setup(x => x.Uri).Returns(new Uri(new Uri("https://base-uri-blob-storage"), "relative-uri-blob-storage"));

        var blobContainerInfo = BlobsModelFactory.BlobContainerInfo(It.IsAny<ETag>(), It.IsAny<DateTimeOffset>());
        var mockContainerResponse = Response.FromValue(blobContainerInfo, new Mock<Response>().Object);

        mockBlobServiceClient.Setup(x => x.GetBlobContainerClient(It.IsAny<string>())).Returns(mockBlobContainerClient.Object);
        mockBlobContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
        var page = Page<BlobItem>.FromValues(BlobItems, continuationToken: null, new Mock<Response>().Object);

        mockBlobContainerClient.Setup<Task<Response<BlobContainerInfo>>>(x =>
            x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(),
            It.IsAny<BlobContainerEncryptionScopeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockContainerResponse);
        mockBlobContainerClient.Setup(x =>
            x.DeleteBlobIfExistsAsync(It.IsAny<string>(), It.IsAny<DeleteSnapshotsOption>(),
            It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue<bool>(true, new Mock<Response>().Object));
        mockBlobClient.Setup(x =>
            x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new RequestFailedException("Exception"));
        var mockBlobItem = AsyncPageable<BlobItem>.FromPages(new[] { page });
        mockBlobContainerClient.Setup(x =>
            x.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(mockBlobItem);

        mockBlobBatchClient = new Mock<IBlobBatchClientWrapper>();
        mockBlobBatchClient.Setup(m => m.SubmitBatchAsync(It.IsAny<BlobBatch>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new BlobService(mockBlobServiceClient.Object, mockBlobBatchClient.Object);

        return service;
    }

    public static Response<BlobContentInfo> AddBlobItems()
    {
        var blobContentInfo = new Mock<BlobContentInfo>();
        var mockContentResponse = Response.FromValue(blobContentInfo.Object, new Mock<Response>().Object);

        BlobItems.Add(BlobsModelFactory.BlobItem("blob-item-1"));
        BlobItems.Add(BlobsModelFactory.BlobItem("blob-item-2"));
        BlobItems.Add(BlobsModelFactory.BlobItem("blob-item-3"));
        return mockContentResponse;
    }
}
