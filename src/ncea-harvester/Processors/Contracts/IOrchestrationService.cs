using Ncea.Harvester.Models;

namespace Ncea.Harvester.Processors.Contracts
{
    public interface IOrchestrationService
    {
        Task SaveHarvestedXmlFiles(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
        Task SendMessagesToHarvestedQueue(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
    }
}
