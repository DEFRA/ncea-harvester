using Ncea.Harvester.BusinessExceptions;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using Ncea.Harvester.Services.Contracts;
using Ncea.Harvester.Utils;
using System.Xml.Linq;

namespace Ncea.Harvester.Processors;

public class MedinProcessor : IProcessor
{
    private int _totalRecordCount;
    private readonly string _dataSourceName;
    private readonly IApiClient _apiClient;
    private readonly IOrchestrationService _orchestrationService;
    private readonly IBackUpService _backUpService;
    private readonly IDeletionService _deletionService;
    private readonly IValidationService _validationService;
    private readonly ILogger _logger;
    private readonly HarvesterConfiguration _harvesterConfiguration;

    public MedinProcessor(IApiClient apiClient,
        IOrchestrationService orchestrationService,
        IBackUpService backUpService,
        IDeletionService deletionService,
        IValidationService validationService,
        ILogger<MedinProcessor> logger,
        HarvesterConfiguration harvesterConfiguration)
    {
        _apiClient = apiClient;
        _harvesterConfiguration = harvesterConfiguration;
        _orchestrationService = orchestrationService;
        _backUpService = backUpService;
        _deletionService = deletionService;
        _logger = logger;
        _validationService = validationService;
        _apiClient.CreateClient(_harvesterConfiguration.DataSourceApiBase);
        _dataSourceName = _harvesterConfiguration.ProcessorType.ToString().ToLowerInvariant();
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var harvestedFiles = new List<HarvestedFile>();

        // Harvest metadata from datasource, Backup the metadata xml blobs from previous run, save the meatadata xml blobs in current run and delete the backed up blobs from previous run
        await HarvestMedinMetadata(harvestedFiles, cancellationToken);

        _logger.LogInformation("Harvester summary | Total record count : {total} | Saved blob count : {itemsSavedSuccessfully} | DataSource : {_dataSourceName}", _totalRecordCount, harvestedFiles.Count(x => !string.IsNullOrWhiteSpace(x.BlobUrl)), _dataSourceName);

        await _orchestrationService.SendMessagesToHarvestedQueue(DataSource.Medin, harvestedFiles, cancellationToken);

        _logger.LogInformation("Harvester summary | Total record count : {total} | Queued item count : {itemsQueuedSuccessfully} | DataSource : {_dataSourceName}", _totalRecordCount, harvestedFiles.Count(x => x.HasMessageSent.GetValueOrDefault(false)), _dataSourceName);
    }

    private async Task HarvestMedinMetadata(List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken)
    {
        var startPosition = 1;
        var maxBatchSize = 100;        
        var hasNextRecords = true;

        _totalRecordCount = await GetTotalRecordCount(cancellationToken);
        _logger.LogInformation("Harvester summary | Total record count : {total} | DataSource : {_dataSourceName}", _totalRecordCount, _dataSourceName);

        await _backUpService.BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(_dataSourceName, cancellationToken);
        
        while (hasNextRecords)
        {
            var responseXml = await GetMedinData(startPosition, maxBatchSize, cancellationToken);
            if (responseXml != null)
            {
                startPosition = GetNextStartPostionInMedinData(out hasNextRecords, out _totalRecordCount, responseXml!);
                await SaveHarvestedRecords(harvestedFiles, hasNextRecords, responseXml, cancellationToken);
            }
            else
            {
                startPosition += maxBatchSize;
            }            

            if (startPosition != 0) hasNextRecords = (startPosition <= _totalRecordCount);
        }

        await _deletionService.DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(_dataSourceName, cancellationToken);

        _logger.LogInformation("Harvester summary | Total record count : {total} | Harvested record count : {itemsHarvestedSuccessfully} | DataSource : {_dataSourceName}", _totalRecordCount, harvestedFiles.Count, _dataSourceName);
    }

    private async Task SaveHarvestedRecords(List<HarvestedFile> harvestedFiles, bool hasNextRecords, XDocument? responseXml, CancellationToken cancellationToken)
    {
        var metaDataXmlNodes = GetMetadataList(responseXml, hasNextRecords);

        if (metaDataXmlNodes != null)
        {
            foreach (var metaDataXmlNode in metaDataXmlNodes)
            {
                var fileIdentifier = GetFileIdentifier(metaDataXmlNode);
                var metaDataXmlString = GetMetadataXmlString(metaDataXmlNode);
                var isMetadataValid = _validationService.IsValid(metaDataXmlNode);
                
                if (isMetadataValid)
                {
                    var harvestedFile = await _orchestrationService.SaveHarvestedXmlFile(_dataSourceName, fileIdentifier!, metaDataXmlString, cancellationToken);
                    harvestedFiles.Add(harvestedFile);
                }
                else
                {
                    var errorMessage = $"One or more mandatory fields does not exist | DataSource : {_dataSourceName} | file-id : {fileIdentifier ?? string.Empty}";
                    harvestedFiles.Add(new HarvestedFile(string.Empty, string.Empty, errorMessage, null));
                    CustomLogger.LogErrorMessage(_logger, errorMessage, null);
                }
            }
        }
    }

    private static string GetMetadataXmlString(XElement metaDataXmlNode)
    {
        string? metaDataXmlString = metaDataXmlNode.ToString();
        metaDataXmlString = string.Concat("<?xml version=\"1.0\" encoding=\"utf-8\"?>", metaDataXmlString);
        return metaDataXmlString;
    }
    
    private async Task<XDocument?> GetMedinData(int startPosition, int maxRecords, CancellationToken cancellationToken)
    {
        XDocument? document = null;
        var apiUrl = _harvesterConfiguration.DataSourceApiUrl;
        apiUrl = apiUrl.Replace("{{maxRecords}}", Convert.ToString(maxRecords)).Replace("{{startPosition}}", Convert.ToString(startPosition));
        
        try
        {
            var responseXmlString = await _apiClient.GetAsync(apiUrl, cancellationToken);
            document = XDocument.Parse(responseXmlString);
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Error occurred while harvesting the metadata for Data source: {_dataSourceName}, start position: {startPosition}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            ThrowExceptionWhenFailureFromInitialRequest(maxRecords, ex, errorMessage);
        }
        catch (TaskCanceledException ex)
        {
            string? errorMessage;
            if (ex.CancellationToken.IsCancellationRequested)
            {
                errorMessage = $"Request was cancelled while harvesting the metadata for Data source: {_dataSourceName}, start position: {startPosition}";
            }
            else
            {
                errorMessage = $"Request timed out while harvesting the metadata for Data source: {_dataSourceName}, start position: {startPosition}";
            }
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            ThrowExceptionWhenFailureFromInitialRequest(maxRecords, ex, errorMessage);
        }
        return document;
    }

    private static List<XElement>? GetMetadataList(XDocument? responseXml, bool hasNextRecords)
    {
        var metadataList = new List<XElement>();
        if ((responseXml == null) || !hasNextRecords) 
            return metadataList;

        var gmdNameSpaceString = "http://www.isotc211.org/2005/gmd";
        var cswNamespace = responseXml.Root!.GetNamespaceOfPrefix("csw")!;
        metadataList = responseXml.Descendants(cswNamespace + "SearchResults")
            .Elements()
            .Where(n => n.Name.Namespace.NamespaceName == gmdNameSpaceString && n.Name.LocalName == "MD_Metadata")
            .ToList();

        return metadataList.Count > 0 ? metadataList : [];
    }

    private static string? GetFileIdentifier(XElement xmlElement)
    {
        var gmdNameSpaceString = "http://www.isotc211.org/2005/gmd";
        var fileIdentifierXmlElement = xmlElement.Descendants()
                               .FirstOrDefault(n => n.Name.Namespace.NamespaceName == gmdNameSpaceString
                                           && n.Name.LocalName == "fileIdentifier");
        var fileIdentifier = fileIdentifierXmlElement?.Descendants()?.FirstOrDefault()?.Value;
        return fileIdentifier;
    }

    private static int GetNextStartPostionInMedinData(out bool hasNextRecords, out int totalRecords, XDocument responseXml)
    {
        var cswNameSpace = "http://www.opengis.net/cat/csw/2.0.2";
        var searchResultsElement = responseXml.Descendants()
                                        .FirstOrDefault(n => n.Name.Namespace.NamespaceName == cswNameSpace
                                                    && n.Name.LocalName == "SearchResults");
        var nextRecordAttribute = searchResultsElement?.Attribute("nextRecord")?.Value;
        var totalRecordAttribute = searchResultsElement?.Attribute("numberOfRecordsMatched")?.Value;
        hasNextRecords = Int32.TryParse(nextRecordAttribute, out int nextRecord);
        bool hasTotalRecords = Int32.TryParse(totalRecordAttribute, out totalRecords);
        hasNextRecords = (hasTotalRecords && hasNextRecords && nextRecord > 0);
        return nextRecord;
    }

    private async Task<int> GetTotalRecordCount(CancellationToken cancellationToken)
    {
        var responseXml = await GetMedinData(1, 1, cancellationToken);
        var cswNameSpace = "http://www.opengis.net/cat/csw/2.0.2";
        var searchResultsElement = responseXml!.Descendants()
                                        .FirstOrDefault(n => n.Name.Namespace.NamespaceName == cswNameSpace
                                                    && n.Name.LocalName == "SearchResults");
        var totalRecords = searchResultsElement?.Attribute("numberOfRecordsMatched")?.Value;
        return int.TryParse(totalRecords, out int result) ? result : 0;
    }

    private static void ThrowExceptionWhenFailureFromInitialRequest(int maxRecords, HttpRequestException ex, string errorMessage)
    {
        if (maxRecords == 1)
        {
            throw new DataSourceConnectionException(errorMessage, ex);
        }
    }
    private static void ThrowExceptionWhenFailureFromInitialRequest(int maxRecords, TaskCanceledException ex, string errorMessage)
    {
        if (maxRecords == 1)
        {
            throw new DataSourceConnectionException(errorMessage, ex);
        }
    }
}
