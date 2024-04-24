using Ncea.Harvester.Infrastructure.Models.Responses;
using Ncea.Harvester.Models;

namespace Ncea.Harvester.Processor.Contracts
{
    public interface IOrchestrationService
    {
        Task SaveHarvestedXmlFiles(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
        Task SendMessagesToHarvestedQueue(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
    }
}
