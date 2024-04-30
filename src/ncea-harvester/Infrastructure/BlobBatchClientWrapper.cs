﻿using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs;
using ncea.harvester.Infrastructure.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace ncea.harvester.Infrastructure;

[ExcludeFromCodeCoverage]
public class BlobBatchClientWrapper : IBlobBatchClientWrapper
{
    private readonly BlobBatchClient _blobBatchClient;
    public BlobBatchClientWrapper(BlobServiceClient blobServiceClient)
    {
        _blobBatchClient = blobServiceClient.GetBlobBatchClient();
    }

    public BlobBatch CreateBatch()
    {
        return _blobBatchClient.CreateBatch();
    }

    public async Task SubmitBatchAsync(BlobBatch blobBatch, bool throwOnFailure, CancellationToken cancellationToken)
    {
        await _blobBatchClient.SubmitBatchAsync(blobBatch, throwOnFailure, cancellationToken);
    }
}