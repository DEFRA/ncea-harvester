using Azure.Storage.Blobs;
using Ncea.Harvester.Infrastructure.Models.Requests;
using System.Runtime.CompilerServices;
using Ncea.Harvester.Infrastructure.Contracts;

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
}
