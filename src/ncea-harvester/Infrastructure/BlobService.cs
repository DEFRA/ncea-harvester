using Azure.Storage.Blobs.Models;
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

    public async IAsyncEnumerable<string> GetListAsync(string container, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(container);
        await foreach (var blob in blobContainer.GetBlobsAsync(cancellationToken: cancellationToken))
            yield return blob.Name;
    }

    public Task<string?> GetAsync(GetBlobRequest request)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(request.Container);
        var blobClient = blobContainer.GetBlobClient(request.Blob);
        return Task.FromResult(blobClient.Uri.AbsoluteUri)!;
    }

    public async IAsyncEnumerable<string?> GetAllAsync(string container,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blob in GetListAsync(container, cancellationToken))
            yield return await GetAsync(new GetBlobRequest(blob, container));
    }

    public async Task<string> SaveAsync(SaveBlobRequest request, CancellationToken cancellationToken)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(request.Container);
        await blobContainer.CreateIfNotExistsAsync(PublicAccessType.BlobContainer, null, cancellationToken);        
        var blobClient = blobContainer.GetBlobClient(request.FileName);
        await blobClient.UploadAsync(request.Blob, cancellationToken);
        return blobClient.Uri.AbsoluteUri;
    }

    public async Task<bool> DeleteAsync(DeleteBlobRequest request, CancellationToken cancellationToken)
    {
        var blobContainer = _blobServiceClient.GetBlobContainerClient(request.Container);
        return (await blobContainer.DeleteBlobIfExistsAsync(request.Blob, cancellationToken: cancellationToken)).Value;
    }
}
