using Ncea.Harvester.Enums;
using Ncea.Harvester.Models;

namespace Ncea.Harvester.Services.Contracts
{
    public interface IOrchestrationService
    {
        Task<HarvestedFile> SaveHarvestedXmlFile(string dataSourceName, string fileIdentifier, string xmlContent, CancellationToken cancellationToken);
        Task SendMessagesToHarvestedQueue(DataSource dataSource, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
    }
}
