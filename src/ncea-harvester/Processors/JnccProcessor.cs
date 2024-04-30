using HtmlAgilityPack;
using ncea.harvester.Services.Contracts;
using Ncea.Harvester.BusinessExceptions;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
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
    private readonly IOrchestrationService _orchestrationService;
    private readonly ILogger _logger;
    private readonly HarvesterConfiguration _harvesterConfiguration;

    public JnccProcessor(IApiClient apiClient,
        IOrchestrationService orchestrationService,
        ILogger<JnccProcessor> logger,
        HarvesterConfiguration harvesterConfiguration)
    {
        _apiClient = apiClient;
        _harvesterConfiguration = harvesterConfiguration;        
        _orchestrationService = orchestrationService;
        _logger = logger;

        _apiClient.CreateClient(_harvesterConfiguration.DataSourceApiBase);
        _dataSourceName = _harvesterConfiguration.ProcessorType.ToString().ToLowerInvariant();
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var harvestedFiles = new List<HarvestedFile>();

        await HarvestJnccMetadataFiles(harvestedFiles, cancellationToken);

        //TO-DO: backup the blobs from previous run

        await _orchestrationService.SaveHarvestedXmlFiles(_dataSourceName, harvestedFiles, cancellationToken);

        //TO-DO: delete the blobs from previous run

        await _orchestrationService.SendMessagesToHarvestedQueue(_dataSourceName, harvestedFiles, cancellationToken);        

        _logger.LogInformation("Harvester summary - Total records : {total} | Success : {itemsHarvestedSuccessfully}", harvestedFiles.Count, harvestedFiles.Count(x => !string.IsNullOrWhiteSpace(x.ErrorMessage)));
    }

    private async Task HarvestJnccMetadataFiles(List<HarvestedFile> harvestedFiles, CancellationToken cancellationToken)
    {
        var responseHtmlString = await GetJnccDataMaster(_harvesterConfiguration.DataSourceApiUrl, cancellationToken);
        var documentLinks = GetDocumentLinks(responseHtmlString);

        foreach (var documentLink in documentLinks)
        {
            var apiUrl = "/waf/" + documentLink;
            var metaDataXmlString = await GetJnccMetadata(apiUrl, documentLink, cancellationToken);
            if (!string.IsNullOrEmpty(metaDataXmlString))
            {
                var documentFileIdentifier = GetFileIdentifier(metaDataXmlString);

                if (!string.IsNullOrWhiteSpace(documentFileIdentifier))
                {
                    harvestedFiles.Add(new HarvestedFile(documentFileIdentifier, string.Empty, metaDataXmlString, string.Empty));
                }
                else
                {
                    var errorMessage = "File Identifier not exists";
                    harvestedFiles.Add(new HarvestedFile(string.Empty, string.Empty, metaDataXmlString, errorMessage));
                    CustomLogger.LogErrorMessage(_logger, errorMessage, null);
                }
            }
            else
            {
                var errorMessage = $"File not found exception : file-id : {documentLink}";
                harvestedFiles.Add(new HarvestedFile(string.Empty, string.Empty, string.Empty, errorMessage));
                CustomLogger.LogErrorMessage(_logger, errorMessage, null);
            }            
        }
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
            return responseXmlString;
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
