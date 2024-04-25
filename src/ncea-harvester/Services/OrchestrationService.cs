using Azure;
using Azure.Messaging.ServiceBus;
using ncea.harvester.Services.Contracts;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Infrastructure.Models.Responses;
using Ncea.Harvester.Models;
using Ncea.Harvester.Utils;
using System.Text;

namespace ncea.harvester.Services;

public class OrchestrationService : IOrchestrationService
{
    private readonly IBlobService _blobService;
    private readonly IServiceBusService _serviceBusService;
    private readonly ILogger _logger;

    public OrchestrationService(IBlobService blobService, IServiceBusService serviceBusService, ILogger<OrchestrationService> logger)
    {
        _blobService = blobService;
        _serviceBusService = serviceBusService;
        _logger = logger;
    }

    public async Task SaveHarvestedXmlFiles(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken)
    {
        foreach (var harvestedFile in harvestedFiles.Where(x => !string.IsNullOrWhiteSpace(x.FileIdentifier)))
        {
            var response = await SaveHarvestedXml(dataSourceName, harvestedFile.FileIdentifier, harvestedFile.FileContent, cancellationToken);
            harvestedFile.BlobUrl = response.BlobUrl;
            harvestedFile.ErrorMessage = response.ErrorMessage;
        }
    }

    public async Task SendMessagesToHarvestedQueue(string dataSourceName, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken)
    {
        foreach (var harvestedFile in harvestedFiles.Where(x => !string.IsNullOrWhiteSpace(x.BlobUrl)))
        {
            var response = await SendMessageToHarvestedQueue(dataSourceName, harvestedFile.FileIdentifier, harvestedFile.FileContent, cancellationToken);
            harvestedFile.ErrorMessage = response.ErrorMessage;
        }
    }

    private async Task<SaveBlobResponse> SaveHarvestedXml(string dataSourceName, string documentFileIdentifier, string metaDataXmlString, CancellationToken cancellationToken)
    {
        var blobUrl = string.Empty;
        var errorMessageBase = "Error occured while saving the file to the blob storage";
        var xmlStream = new MemoryStream(Encoding.ASCII.GetBytes(metaDataXmlString));
        var documentFileName = string.Concat(documentFileIdentifier, ".xml");

        try
        {
            blobUrl = await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, documentFileName, dataSourceName), cancellationToken);
            return new SaveBlobResponse(documentFileIdentifier, blobUrl, string.Empty);
        }
        catch (RequestFailedException ex)
        {
            var errorMessage = $"{errorMessageBase}: for datasource: {dataSourceName}, file-id: {documentFileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            return new SaveBlobResponse(documentFileIdentifier, blobUrl, errorMessageBase);
        }
    }

    private async Task<SendMessageResponse> SendMessageToHarvestedQueue(string documentFileIdentifier, string dataSourceName, string metaDataXmlString, CancellationToken cancellationToken)
    {
        var errorMessageBase = "Error occured while sending message to harvested-queue";
        try
        {
            await _serviceBusService.SendMessageAsync(new SendMessageRequest(dataSourceName, documentFileIdentifier, metaDataXmlString), cancellationToken);

            return new SendMessageResponse(documentFileIdentifier, true, string.Empty);
        }
        catch (ServiceBusException ex)
        {
            var errorMessage = $"{errorMessageBase}: for datasource: {dataSourceName}, file-id: {documentFileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            return new SendMessageResponse(documentFileIdentifier, false, errorMessageBase);
        }
    }
}
