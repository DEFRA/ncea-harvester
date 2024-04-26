using Ncea.Harvester.Infrastructure.Models.Requests;

namespace Ncea.Harvester.Infrastructure.Contracts;

public interface IServiceBusService
{
    Task SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken);
}
