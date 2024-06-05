using HtmlAgilityPack;
using Ncea.Harvester.BusinessExceptions;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using Ncea.Harvester.Services.Contracts;
using Ncea.Harvester.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace Ncea.Harvester.Processors;

public class JnccProcessor : IProcessor
{
    private readonly string _dataSourceName;
    private readonly IApiClient _apiClient;
    private readonly IBackUpService _backUpService;
    private readonly IDeletionService _deletionService;
    private readonly IOrchestrationService _orchestrationService;
    private readonly HarvesterConfiguration _harvesterConfiguration;
    private readonly ILogger _logger;

    public JnccProcessor(IApiClient apiClient,
        IOrchestrationService orchestrationService,
        IBackUpService backUpService,
        IDeletionService deletionService,
        ILogger<JnccProcessor> logger,
        HarvesterConfiguration harvesterConfiguration)
    {
        _apiClient = apiClient;
        _backUpService = backUpService;
        _deletionService = deletionService;
        _orchestrationService = orchestrationService;
        _harvesterConfiguration = harvesterConfiguration;
        _logger = logger;

        _apiClient.CreateClient(_harvesterConfiguration.DataSourceApiBase);
        _dataSourceName = _harvesterConfiguration.ProcessorType.ToString().ToLowerInvariant();
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var harvestedFiles = new List<HarvestedFile>();

        // Harvest metadata from datasource, Backup the metadata xml blobs from previous run, save the meatadata xml blobs in current run, delete the backed up blobs from previous run
        await HarvestJnccMetadataFiles(harvestedFiles, cancellationToken);        

        _logger.LogInformation("Harvester summary | Total record count : {total} | Saved blob count : {itemsSavedSuccessfully} | DataSource : {_dataSourceName}", harvestedFiles.Count, harvestedFiles.Count(x => !string.IsNullOrWhiteSpace(x.BlobUrl)), _dataSourceName);

        // Backup the enriched xml files from previous run, send sb message with meatadata xml content from current run, delete the backed up the enriched xml files from previous run
        _backUpService.BackUpEnrichedXmlFilesCreatedInPreviousRun(_dataSourceName);
        await _orchestrationService.SendMessagesToHarvestedQueue(DataSource.Jncc, harvestedFiles, cancellationToken);
        _deletionService.DeleteEnrichedXmlFilesCreatedInPreviousRun(_dataSourceName); 
        
        _logger.LogInformation("Harvester summary | Total record count : {total} | Queued item count : {itemsQueuedSuccessfully} | DataSource : {_dataSourceName}", harvestedFiles.Count, harvestedFiles.Count(x => x.HasMessageSent.GetValueOrDefault(false)), _dataSourceName);
    }

    private async Task HarvestJnccMetadataFiles(List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken)
    {
        var responseHtmlString = await GetJnccDataMaster(_harvesterConfiguration.DataSourceApiUrl, cancellationToken);
        var documentLinks = GetDocumentLinks(responseHtmlString);

        _logger.LogInformation("Harvester summary | Total record count : {total} | DataSource : {_dataSourceName}", documentLinks.Count, _dataSourceName);

        await _backUpService.BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(_dataSourceName, cancellationToken);

        foreach (var documentLink in documentLinks)
        {
            var apiUrl = "/waf/" + documentLink;
            var metaDataXmlString = await GetJnccMetadata(apiUrl, documentLink, cancellationToken);
            if (!string.IsNullOrEmpty(metaDataXmlString))
            {
                var fileIdentifier = GetFileIdentifier(metaDataXmlString);

                if (!string.IsNullOrWhiteSpace(fileIdentifier))
                {
                    var harvestedFile = await _orchestrationService.SaveHarvestedXmlFile(_dataSourceName, fileIdentifier, metaDataXmlString, cancellationToken);
                    harvestedFiles.Add(harvestedFile);
                }
                else
                {
                    var errorMessage = "File Identifier not exists | DataSource : {_dataSourceName}";
                    harvestedFiles.Add(new HarvestedFile(string.Empty, string.Empty, errorMessage, null));
                    CustomLogger.LogErrorMessage(_logger, errorMessage, null);
                }
            }
            else
            {
                var errorMessage = $"File not found exception : file-id : {documentLink}, DataSource : {_dataSourceName}";
                harvestedFiles.Add(new HarvestedFile(string.Empty, string.Empty, errorMessage, null));
                CustomLogger.LogErrorMessage(_logger, errorMessage, null);
            }            
        }

        await _deletionService.DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(_dataSourceName, cancellationToken);
    }

    private async Task<string> GetJnccDataMaster(string apiUrl, CancellationToken cancellationToken)
    {
        try
        {
            var responseXmlString = await _apiClient.GetAsync(apiUrl, cancellationToken);
            return responseXmlString;
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Error occured while harvesting the metadata for Data source: {_dataSourceName}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            throw new DataSourceConnectionException(errorMessage, ex);
        }
        catch (TaskCanceledException ex)
        {
            string? errorMessage;
            if (ex.CancellationToken.IsCancellationRequested)
            {
                errorMessage = $"Request was cancelled while harvesting the metadata for Data source: {_dataSourceName}";
            }
            else
            {
                errorMessage = $"Request timed out while harvesting the metadata for Data source: {_dataSourceName}";
            }
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            throw new DataSourceConnectionException(errorMessage, ex);
        }
    }

    public virtual async Task<string> GetJnccMetadata(string apiUrl, string jnccFileName, CancellationToken cancellationToken)
    {
        var responseXmlString = string.Empty;
        try
        {
            responseXmlString = await _apiClient.GetAsync(apiUrl, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Error occured while harvesting the metadata for Data source: {_dataSourceName}, file-id: {jnccFileName}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            if (ex.StatusCode != HttpStatusCode.NotFound)
            {
                throw new DataSourceConnectionException(errorMessage, ex);
            }            
        }
        catch (TaskCanceledException ex)
        {
            string? errorMessage;
            if (ex.CancellationToken.IsCancellationRequested)
            {
                errorMessage = $"Request was cancelled while harvesting the metadata for Data source: {_dataSourceName}, file-id: {jnccFileName}";
            }
            else
            {
                errorMessage = $"Request timed out while harvesting the metadata for Data source: {_dataSourceName}, file-id: {jnccFileName}";
            }
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            throw new DataSourceConnectionException(errorMessage, ex);
        }

        return responseXmlString;
    }

    private static List<string> GetDocumentLinks(string responseHtmlString)
    {
        var documentLinks = new List<string>();
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(responseHtmlString);

        var anchorNodes = htmlDocument.DocumentNode?.SelectNodes("//a[@href]");
        if (anchorNodes != null)
        {
            foreach (var anchorNode in anchorNodes)
            {
                var hrefValue = anchorNode.GetAttributeValue("href", "");
                documentLinks.Add(hrefValue);
            }
        }
        return documentLinks;
    }

    [ExcludeFromCodeCoverage]
    private static string? GetFileIdentifier(string xmlString)
    {        
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);
        var xDoc = XDocument.Load(xmlDoc!.CreateNavigator()!.ReadSubtree());
        var xmlElement = xDoc.Root;

        string gmdNameSpaceString = "http://www.isotc211.org/2005/gmd";
        var fileIdentifierXmlElement = xmlElement!.Descendants()
                                                  .FirstOrDefault(n => n.Name.Namespace.NamespaceName == gmdNameSpaceString
                                                                  && n.Name.LocalName == "fileIdentifier");
        var fileIdentifier = fileIdentifierXmlElement?.Descendants()?.FirstOrDefault()?.Value;
        return fileIdentifier;
    }
}
