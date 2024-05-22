﻿using Azure;
using Azure.Messaging.ServiceBus;
using Ncea.harvester.Services.Contracts;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Infrastructure.Models.Responses;
using Ncea.Harvester.Models;
using Ncea.Harvester.Utils;
using System.Text;
using System.Text.Json;

namespace Ncea.harvester.Services;

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

    public async Task SendMessagesToHarvestedQueue(DataSource dataSource, List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken)
    {
        var dataStandard = (dataSource == DataSource.Jncc) ? DataStandard.Gemini22 : DataStandard.Gemini23;

        foreach (var harvestedFile in harvestedFiles.Where(x => !string.IsNullOrWhiteSpace(x.BlobUrl)))
        {
            var response = await SendMessageToHarvestedQueue(new HarvestedRecordMessage(harvestedFile.FileIdentifier, DataFormat.Xml, dataStandard, dataSource), cancellationToken);
            harvestedFile.ErrorMessage = response.ErrorMessage;
            harvestedFile.HasMessageSent = response.IsSucceeded;
        }
    }

    private async Task<SaveBlobResponse> SaveHarvestedXml(string dataSourceName, string documentFileIdentifier, string metaDataXmlString, CancellationToken cancellationToken)
    {
        var blobUrl = string.Empty;
        var errorMessageBase = "Error occured while saving the file to the blob storage";
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
            var message = JsonSerializer.Serialize(harvestedRecord);
            await _serviceBusService.SendMessageAsync(new SendMessageRequest(message), cancellationToken);
            isSuceeded = true;
        }
        catch (ServiceBusException ex)
        {
            errorMessageBase = "Error occured while sending message to harvested-queue";
            var errorMessage = $"{errorMessageBase}: for datasource: {harvestedRecord.DataSource}, file-id: {harvestedRecord.FileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            return new SendMessageResponse(harvestedRecord.FileIdentifier, false, errorMessageBase);
        }

        return new SendMessageResponse(harvestedRecord.FileIdentifier, isSuceeded, errorMessageBase);
    }
}
