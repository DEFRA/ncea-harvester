﻿using Azure.Storage.Blobs;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Infrastructure.Contracts;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using ncea.harvester.Infrastructure.Contracts;
using System.Reflection.Metadata;
using Azure.Core;

namespace Ncea.Harvester.Infrastructure;

public class BlobService : IBlobService
{
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
        // Create a batch
        BlobBatch batch = _blobBatchClient.CreateBatch();

        var sourceContainer = _blobServiceClient.GetBlobContainerClient(request.SourceContainer);
        var targetContainer = _blobServiceClient.GetBlobContainerClient(request.DestinationContainer);
        await targetContainer.CreateIfNotExistsAsync(PublicAccessType.None, null, null, cancellationToken);

        var blobs = sourceContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, "", cancellationToken);
        await foreach (BlobItem blob in blobs)
        {
            var blobUri = sourceContainer.GetBlobClient(blob.Name);
            var newBlob = targetContainer.GetBlobClient(blob.Name);
            await newBlob.StartCopyFromUriAsync(blobUri.Uri, null, cancellationToken);
            blobItems.Add(blob.Name);            
        }

        foreach(var blobItem in blobItems)
        {
            batch.DeleteBlob(request.SourceContainer, blobItem, DeleteSnapshotsOption.None, null);
        }

        await _blobBatchClient.SubmitBatchAsync(batch, true, cancellationToken);
    }

    public async Task DeleteBlobsAsync(string containerName, CancellationToken cancellationToken)
    {
        var blobItems = new List<string>();
        // Create a batch
        BlobBatch batch = _blobBatchClient.CreateBatch();

        var sourceContainer = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobs = sourceContainer.GetBlobsAsync(BlobTraits.None, BlobStates.None, "", cancellationToken);
        await foreach (BlobItem blob in blobs)
        {
            blobItems.Add(blob.Name);
        }

        foreach (var blobItem in blobItems)
        {
            batch.DeleteBlob(containerName, blobItem, DeleteSnapshotsOption.None, null);
        }

        await _blobBatchClient.SubmitBatchAsync(batch, true, cancellationToken);
    }
}
