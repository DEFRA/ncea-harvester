using Ncea.Harvester.Infrastructure.Models.Responses;
using Ncea.Harvester.Models;

namespace Ncea.Harvester.Processor.Contracts
{
    public interface IOrchestrationService
    {
        public Task<SaveBlobResponse> SaveHarvestedXml(string dataSourceName, string documentFileIdentifier, string metaDataXmlString, CancellationToken cancellationToken);
        Task SendMessagesToHarvestedQueue(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken);
    }
}
