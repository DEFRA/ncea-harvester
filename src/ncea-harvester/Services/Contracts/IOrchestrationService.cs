using Ncea.Harvester.Enums;
using Ncea.Harvester.Models;

namespace Ncea.harvester.Services.Contracts
{
    public interface IOrchestrationService
    {
        Task SaveHarvestedXmlFiles(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
        Task SendMessagesToHarvestedQueue(DataSource dataSource, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
    }
}
