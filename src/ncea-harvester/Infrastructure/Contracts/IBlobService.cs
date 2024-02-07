using Ncea.Harvester.Infrastructure.Models.Requests;

namespace Ncea.Harvester.Infrastructure.Contracts;

public interface IBlobService
{
    IAsyncEnumerable<string> GetListAsync(string container, CancellationToken cancellationToken);

    Task<string?> GetAsync(GetBlobRequest request);

    IAsyncEnumerable<string?> GetAllAsync(string container, CancellationToken cancellationToken);

    Task<string> SaveAsync(SaveBlobRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(DeleteBlobRequest request, CancellationToken cancellationToken);
}
