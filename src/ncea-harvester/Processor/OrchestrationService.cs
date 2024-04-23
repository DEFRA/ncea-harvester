using Azure;
using Azure.Messaging.ServiceBus;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Infrastructure.Models.Responses;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processor.Contracts;
using Ncea.Harvester.Utils;
using System.Text;

namespace Ncea.Harvester.Processor;

public class OrchestrationService : IOrchestrationService
{
    private readonly IBlobService _blobService;
    private readonly IServiceBusService _serviceBusService;
    private readonly ILogger _logger;

    public OrchestrationService(IBlobService blobService, IServiceBusService serviceBusService, ILogger logger)
    {
        _blobService = blobService;
        _serviceBusService = serviceBusService;
        _logger = logger;
    }

    public async Task<SaveBlobResponse> SaveHarvestedXml(string documentFileIdentifier, string dataSourceName, string metaDataXmlString)
    {
        var blobUrl = string.Empty;
        var errorMessageBase = "Error occured while saving the file to the blob storage";
        var xmlStream = new MemoryStream(Encoding.ASCII.GetBytes(metaDataXmlString));
        var documentFileName = string.Concat(documentFileIdentifier, ".xml");

        try
        {
            blobUrl = await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, documentFileName, dataSourceName), CancellationToken.None);
        }
        catch (RequestFailedException ex)
        {
            var errorMessage = $"{errorMessageBase}: for datasource: {dataSourceName}, file-id: {documentFileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
        }

        return new SaveBlobResponse(documentFileIdentifier, blobUrl, errorMessageBase);
    }

    public async Task SendMessagesToHarvestedQueue(string dataSourceName, List<HarvestedFile> harvestedFiles)
    {
        foreach (var harvestedFile in harvestedFiles.Where(x => !string.IsNullOrWhiteSpace(x.BlobUrl)))
        {
            var response = await SendMessageToHarvestedQueue(dataSourceName, harvestedFile.FileIdentifier, harvestedFile.FileContent);
            harvestedFile.ErrorMessage = response.ErrorMessage;
        }
    }

    private async Task<SendMessageResponse> SendMessageToHarvestedQueue(string documentFileIdentifier, string dataSourceName, string metaDataXmlString)
    {
        var errorMessageBase = "Error occured while sending message to harvested-queue";
        try
        {
            await _serviceBusService.SendMessageAsync(metaDataXmlString);

            return new SendMessageResponse(documentFileIdentifier, true, string.Empty);
        }
        catch (ServiceBusException ex)
        {
            var errorMessage = $"{errorMessageBase}: for datasource: {dataSourceName}, file-id: {documentFileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
        }

        return new SendMessageResponse(documentFileIdentifier, false, errorMessageBase);
    }
}
