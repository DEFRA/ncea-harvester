using Ncea.Harvester.Infrastructure.Models.Requests;

namespace Ncea.Harvester.Infrastructure.Contracts;

public interface IBlobService
{
    Task<string> SaveAsync(SaveBlobRequest request, CancellationToken cancellationToken);
}
