using Azure;
using Azure.Messaging.ServiceBus;
using ncea.harvester.Enums;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Infrastructure.Models.Responses;
using Ncea.Harvester.Models;
using Ncea.Harvester.Services.Contracts;
using Ncea.Harvester.Utils;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ncea.Harvester.Services;

public class OrchestrationService : IOrchestrationService
{
    private readonly IBlobService _blobService;
    private readonly IServiceBusService _serviceBusService;
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly ILogger _logger;

    public OrchestrationService(IBlobService blobService, IServiceBusService serviceBusService, ILogger<OrchestrationService> logger)
    {
        _blobService = blobService;
        _serviceBusService = serviceBusService;
        _logger = logger;
    }

    public async Task<HarvestedFile> SaveHarvestedXmlFile(string dataSourceName, string fileIdentifier, string xmlContent, CancellationToken cancellationToken)
    {
        var response = await SaveHarvestedXml(dataSourceName, fileIdentifier, xmlContent, cancellationToken);
        return new HarvestedFile(fileIdentifier, response.BlobUrl, response.ErrorMessage, null);
    }

    public async Task SendMessagesToHarvestedQueue(DataSource dataSource, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken)
    {
        var dataStandard = (dataSource == DataSource.Jncc) ? DataStandard.Gemini22 : DataStandard.Gemini23;

        await SendMessageToHarvestedQueue(new HarvestedRecordMessage(string.Empty, DataFormat.Xml, dataStandard, dataSource, MessageType.Start), cancellationToken);

        foreach (var harvestedFile in harvestedFiles.Where(x => !string.IsNullOrWhiteSpace(x.BlobUrl)))
        {
            var response = await SendMessageToHarvestedQueue(new HarvestedRecordMessage(harvestedFile.FileIdentifier, DataFormat.Xml, dataStandard, dataSource, MessageType.Metadata), cancellationToken);
            harvestedFile.ErrorMessage = response.ErrorMessage;
            harvestedFile.HasMessageSent = response.IsSucceeded;
        }

        await SendMessageToHarvestedQueue(new HarvestedRecordMessage(string.Empty, DataFormat.Xml, dataStandard, dataSource, MessageType.End), cancellationToken);
    }

    private async Task<SaveBlobResponse> SaveHarvestedXml(string dataSourceName, string documentFileIdentifier, string metaDataXmlString, CancellationToken cancellationToken)
    {
        var blobUrl = string.Empty;
        var errorMessageBase = "Error occurred while saving the file to the blob storage";
        var xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(metaDataXmlString));
        var documentFileName = string.Concat(documentFileIdentifier, ".xml");

        try
        {
            blobUrl = await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, documentFileName, dataSourceName), cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            var errorMessage = $"{errorMessageBase}: for datasource: {dataSourceName}, file-id: {documentFileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);            
        }

        return new SaveBlobResponse(documentFileIdentifier, blobUrl, (blobUrl == string.Empty) ? errorMessageBase : string.Empty);
    }

    private async Task<SendMessageResponse> SendMessageToHarvestedQueue(HarvestedRecordMessage harvestedRecord, CancellationToken cancellationToken)
    {
        bool isSuceeded;
        var errorMessageBase = string.Empty;
        
        try
        {
            var message = JsonSerializer.Serialize(harvestedRecord, _serializerOptions);
            await _serviceBusService.SendMessageAsync(new SendMessageRequest(message), cancellationToken);
            isSuceeded = true;
        }
        catch (ServiceBusException ex)
        {
            errorMessageBase = "Error occurred while sending message to harvested-queue";
            var errorMessage = $"{errorMessageBase}: for datasource: {harvestedRecord.DataSource}, file-id: {harvestedRecord.FileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            return new SendMessageResponse(harvestedRecord.FileIdentifier, false, errorMessageBase);
        }

        return new SendMessageResponse(harvestedRecord.FileIdentifier, isSuceeded, errorMessageBase);
    }
}
