using Ncea.Harvester.Infrastructure.Models.Requests;

namespace Ncea.Harvester.Infrastructure.Contracts;

public interface IBlobService
{
    Task<string> SaveAsync(SaveBlobRequest request, CancellationToken cancellationToken);
    Task BackUpContainerAsync(BackUpContainerRequest request, CancellationToken cancellationToken);
    Task DeleteBlobsAsync(string containerName, CancellationToken cancellationToken);
}
