using Azure.Storage.Blobs;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Infrastructure.Contracts;
using Azure.Storage.Blobs.Models;
using Ncea.harvester.Infrastructure.Contracts;
using Ncea.harvester.Extensions;

namespace Ncea.Harvester.Infrastructure;

public class BlobService : IBlobService
{
    private const int MaxBatchSize = 256;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IBlobBatchClientWrapper _blobBatchClient;

    public BlobService(BlobServiceClient blobServiceClient, IBlobBatchClientWrapper blobBatchClient) =>
        (_blobServiceClient, _blobBatchClient) = (blobServiceClient, blobBatchClient);

    public async Task<string> SaveAsync(SaveBlobRequest request, CancellationToken cancellationToken)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(request.Container);
        var blobClient = blobContainer.GetBlobClient(request.FileName);
        await blobClient.UploadAsync(request.Blob, true, cancellationToken);        
        return blobClient.Uri.AbsoluteUri;
    }

    public async Task BackUpContainerAsync(BackUpContainerRequest request, CancellationToken cancellationToken)
    {
        var blobItems = new List<string>();        

        var sourceContainer = _blobServiceClient.GetBlobContainerClient(request.SourceContainer);
        var blobs = sourceContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, "", cancellationToken);

        var targetContainer = _blobServiceClient.GetBlobContainerClient(request.DestinationContainer);
        await targetContainer.CreateIfNotExistsAsync(PublicAccessType.None, null, null, cancellationToken);
       
        await foreach (BlobItem blob in blobs)
        {
            var sourceBlob = sourceContainer.GetBlobClient(blob.Name);
            var targetBlob = targetContainer.GetBlobClient(blob.Name);
            await targetBlob.StartCopyFromUriAsync(sourceBlob.Uri, null, cancellationToken);
            blobItems.Add(blob.Name);
        }

        await DeleteBlobsInBatches(request.SourceContainer, blobItems, cancellationToken);
    }  

    public async Task DeleteBlobsAsync(string containerName, CancellationToken cancellationToken)
    {
        var blobItems = new List<string>();

        var backupContainer = _blobServiceClient.GetBlobContainerClient(containerName);
        if(await backupContainer.ExistsAsync(cancellationToken))
        {
            var blobs = backupContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, "", cancellationToken);
            await foreach (BlobItem blob in blobs)
            {
                blobItems.Add(blob.Name);
            }

            await DeleteBlobsInBatches(containerName, blobItems, cancellationToken);
            await backupContainer.DeleteAsync(null, cancellationToken);
        }        
    }

    private async Task DeleteBlobsInBatches(string containerName, List<string> blobItems, CancellationToken cancellationToken)
    {
        var batchOfItems = blobItems.Batch(MaxBatchSize);
        foreach (var batchItem in batchOfItems)
        {
            var batch = _blobBatchClient.CreateBatch();
            foreach (var blobItem in batchItem)
            {
                batch.DeleteBlob(containerName, blobItem, DeleteSnapshotsOption.None, null);
            }
            await _blobBatchClient.SubmitBatchAsync(batch, true, cancellationToken);
        }
    }
}
