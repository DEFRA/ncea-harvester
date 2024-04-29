using Azure.Storage.Blobs;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Infrastructure.Contracts;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace Ncea.Harvester.Infrastructure;

public class BlobService : IBlobService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(BlobServiceClient blobServiceClient) =>
        (_blobServiceClient) = (blobServiceClient);

    public async Task<string> SaveAsync(SaveBlobRequest request, CancellationToken cancellationToken)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(request.Container);
        var blobClient = blobContainer.GetBlobClient(request.FileName);
        await blobClient.UploadAsync(request.Blob, true, cancellationToken);        
        return blobClient.Uri.AbsoluteUri;
    }

    public async Task BackUpContainerAsync(BackUpContainerRequest request, CancellationToken cancellationToken)
    {
        // Create a batch client
        BlobBatchClient batchClient = _blobServiceClient.GetBlobBatchClient();

        // Create a batch
        BlobBatch batch = batchClient.CreateBatch();

        var sourceContainer = _blobServiceClient.GetBlobContainerClient(request.SourceContainer);
        var targetContainer = _blobServiceClient.GetBlobContainerClient(request.DestinationContainer);
        targetContainer.CreateIfNotExists();

        var blobs = sourceContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, "", cancellationToken);
        await foreach (BlobItem blob in blobs)
        {
            var blobUri = sourceContainer.GetBlobClient(blob.Name);
            var newBlob = targetContainer.GetBlobClient(blob.Name);
            await newBlob.StartCopyFromUriAsync(blobUri.Uri);
            batch.DeleteBlob(request.SourceContainer, blob.Name, DeleteSnapshotsOption.None, null);
        }

        await batchClient.SubmitBatchAsync(batch);
    }

    public async Task DeleteBlobsAsync(string containerName, CancellationToken cancellationToken)
    {
        // Create a batch client
        BlobBatchClient batchClient = _blobServiceClient.GetBlobBatchClient();

        // Create a batch
        BlobBatch batch = batchClient.CreateBatch();

        var sourceContainer = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobs = sourceContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, "", cancellationToken);
        await foreach (BlobItem blob in blobs)
        {            
            batch.DeleteBlob(containerName, blob.Name, DeleteSnapshotsOption.None, null);
        }

        await batchClient.SubmitBatchAsync(batch);
    }
}
