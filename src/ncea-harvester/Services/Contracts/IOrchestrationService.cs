using Ncea.Harvester.Models;

namespace ncea.harvester.Services.Contracts
{
    public interface IOrchestrationService
    {
        Task SaveHarvestedXmlFiles(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
        Task SendMessagesToHarvestedQueue(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
    }
}
