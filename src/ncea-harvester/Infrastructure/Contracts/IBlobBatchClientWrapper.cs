using Azure.Storage.Blobs.Specialized;

namespace ncea.harvester.Infrastructure.Contracts;

public interface IBlobBatchClientWrapper
{
    BlobBatch CreateBatch();
    Task SubmitBatchAsync(BlobBatch blobBatch, bool throwOnFailure, CancellationToken cancellationToken);
}
