using ncea.harvester.Services.Contracts;
using Ncea.Harvester.BusinessExceptions;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using Ncea.Harvester.Utils;
using System.Xml.Linq;

namespace Ncea.Harvester.Processors;

public class MedinProcessor : IProcessor
{
    private readonly string _dataSourceName;
    private readonly IApiClient _apiClient;
    private readonly IOrchestrationService _orchestrationService;
    private readonly IBackUpService _backUpService;
    private readonly IDeletionService _deletionService;
    private readonly ILogger _logger;
    private readonly HarvesterConfiguration _harvesterConfiguration;

    public MedinProcessor(IApiClient apiClient,
        IOrchestrationService orchestrationService,
        IBackUpService backUpService,
        IDeletionService deletionService,
        ILogger<MedinProcessor> logger,
        HarvesterConfiguration harvesterConfiguration)
    {
        _apiClient = apiClient;
        _harvesterConfiguration = harvesterConfiguration;
        _orchestrationService = orchestrationService;
        _backUpService = backUpService;
        _deletionService = deletionService;
        _logger = logger;

        _apiClient.CreateClient(_harvesterConfiguration.DataSourceApiBase);
        _dataSourceName = _harvesterConfiguration.ProcessorType.ToString().ToLowerInvariant();
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var harvestedFiles = new List<HarvestedFile>();

        // Harvest metadata from datasource
        await HarvestMedinMetadata(harvestedFiles, cancellationToken);

        // Backup the metadata xml blobs from previous run, save the meatadata xml blobs in current run, delete the backed up blobs from previous run
        await _backUpService.BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(_dataSourceName, cancellationToken);
        await _orchestrationService.SaveHarvestedXmlFiles(_dataSourceName, harvestedFiles, cancellationToken);
        await _deletionService.DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(_dataSourceName, cancellationToken);

        _logger.LogInformation("Harvester summary | Total record count : {total} | Saved blob count : {itemsSavedSuccessfully} | DataSource : {_dataSourceName}", harvestedFiles.Count, harvestedFiles.Count(x => !string.IsNullOrWhiteSpace(x.BlobUrl)), _dataSourceName);

        // Backup the enriched xml files from previous run, send sb message with meatadata xml content from current run, delete the backed up the enriched xml files from previous run
        _backUpService.BackUpEnrichedXmlFilesCreatedInPreviousRun(_dataSourceName);
        await _orchestrationService.SendMessagesToHarvestedQueue(_dataSourceName, harvestedFiles, cancellationToken);
        _deletionService.DeleteEnrichedXmlFilesCreatedInPreviousRun(_dataSourceName);

        _logger.LogInformation("Harvester summary | Total record count : {total} | Queued item count : {itemsQueuedSuccessfully} | DataSource : {_dataSourceName}", harvestedFiles.Count, harvestedFiles.Count(x => x.HasMessageSent.GetValueOrDefault(false)), _dataSourceName);
    }

    private async Task HarvestMedinMetadata(List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken)
    {
        var startPosition = 1;
        var maxRecords = 100;
        var totalRecords = 0;
        var hasNextRecords = true;

        while (hasNextRecords)
        {
            var responseXml = await GetMedinData(startPosition, maxRecords, cancellationToken);
            startPosition = GetNextStartPostionInMedinData(out hasNextRecords, out totalRecords, responseXml!);
            
            if (startPosition == 1)
            {
                _logger.LogInformation("Harvester summary | Total record count : {total} | DataSource : {_dataSourceName}", totalRecords, _dataSourceName);
            }
            
            var metaDataXmlNodes = GetMetadataList(responseXml, hasNextRecords);

            if (metaDataXmlNodes != null)
            {
                foreach (var metaDataXmlNode in metaDataXmlNodes)
                {
                    var documentFileIdentifier = GetFileIdentifier(metaDataXmlNode);
                    var metaDataXmlString = GetMetadataXmlString(metaDataXmlNode);

                    if (!string.IsNullOrWhiteSpace(documentFileIdentifier))
                    {
                        harvestedFiles.Add(new HarvestedFile(documentFileIdentifier, string.Empty, metaDataXmlString, string.Empty, null));
                    }
                    else
                    {
                        var errorMessage = "File Identifier not exists";
                        harvestedFiles.Add(new HarvestedFile(string.Empty, string.Empty, metaDataXmlString, errorMessage, null));
                        CustomLogger.LogErrorMessage(_logger, errorMessage, null);
                    }
                }
            }

            if (startPosition != 0) hasNextRecords = (startPosition <= totalRecords);
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
        var apiUrl = _harvesterConfiguration.DataSourceApiUrl;
        apiUrl = apiUrl.Replace("{{maxRecords}}", Convert.ToString(maxRecords)).Replace("{{startPosition}}", Convert.ToString(startPosition));

        XDocument? responseDocument;
        try
        {
            var responseXmlString = await _apiClient.GetAsync(apiUrl, cancellationToken);
            responseDocument = XDocument.Parse(responseXmlString);
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Error occured while harvesting the metadata for Data source: {_dataSourceName}, start position: {startPosition}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            throw new DataSourceConnectionException(errorMessage, ex);
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
            throw new DataSourceConnectionException(errorMessage, ex);
        }
        return responseDocument;
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
}
