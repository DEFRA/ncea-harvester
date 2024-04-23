using Azure;
using Azure.Messaging.ServiceBus;
using HtmlAgilityPack;
using Ncea.Harvester.BusinessExceptions;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Infrastructure.Models.Responses;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using Ncea.Harvester.Utils;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Ncea.Harvester.Processors;

public class JnccProcessor : IProcessor
{
    private readonly string _dataSourceName;
    private readonly IApiClient _apiClient;
    private readonly IServiceBusService _serviceBusService;
    private readonly IBlobService _blobService;
    private readonly ILogger _logger;
    private readonly HarvesterConfiguration _harvesterConfiguration;

    public JnccProcessor(IApiClient apiClient,
        IServiceBusService serviceBusService,
        IBlobService blobService,
        ILogger<JnccProcessor> logger,
        HarvesterConfiguration harvesterConfiguration)
    {
        _apiClient = apiClient;
        _harvesterConfiguration = harvesterConfiguration;
        _apiClient.CreateClient(_harvesterConfiguration.DataSourceApiBase);
        _serviceBusService = serviceBusService;
        _logger = logger;
        _blobService = blobService;

        _dataSourceName = _harvesterConfiguration.ProcessorType.ToString().ToLowerInvariant();
    }
    public async Task Process()
    {
        var responseHtmlString = await GetJnccData(_harvesterConfiguration.DataSourceApiUrl);
        var documentLinks = GetDocumentLinks(responseHtmlString);

        var harvestedFiles = new List<HarvestedFile>();

        foreach (var documentLink in documentLinks)
        {
            var apiUrl = "/waf/" + documentLink;
            var metaDataXmlString = await GetJnccMetadata(apiUrl, documentLink);
            var documentFileIdentifier = GetFileIdentifier(metaDataXmlString);

            if (!string.IsNullOrWhiteSpace(documentFileIdentifier))
            {
                var response = await SaveMetadataXml(documentFileIdentifier, metaDataXmlString);
                harvestedFiles.Add(new HarvestedFile(documentFileIdentifier, response.BlobUrl, metaDataXmlString, response.ErrorMessage));
            }
            else
            {
                var errorMessage = "File Identifier not exists";
                harvestedFiles.Add(new HarvestedFile(string.Empty, string.Empty, metaDataXmlString, errorMessage));
                CustomLogger.LogErrorMessage(_logger, errorMessage, null);
            }
        }

        await SendMessagesToHarvestedQueue(harvestedFiles);
    }

    private async Task SendMessagesToHarvestedQueue(List<HarvestedFile> harvestedFiles)
    {
        foreach (var harvestedFile in harvestedFiles.Where(x => !string.IsNullOrWhiteSpace(x.BlobUrl)))
        {
            var response = await SendServiceBusMessage(harvestedFile.FileIdentifier, harvestedFile.FileContent);
            harvestedFile.ErrorMessage = response.ErrorMessage;
        }
    }

    private async Task<SaveBlobResponse> SaveMetadataXml(string documentFileIdentifier, string metaDataXmlString)
    {
        var blobUrl = string.Empty;
        var errorMessageBase = "Error occured while saving the file to the blob storage";
        var xmlStream = new MemoryStream(Encoding.ASCII.GetBytes(metaDataXmlString));
        var documentFileName = string.Concat(documentFileIdentifier, ".xml");

        try
        {
            blobUrl = await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, documentFileName, _dataSourceName), CancellationToken.None);
        }
        catch (RequestFailedException ex)
        {
            var errorMessage = $"{errorMessageBase}: for datasource: {_dataSourceName}, file-id: {documentFileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
        }

        return new SaveBlobResponse(documentFileIdentifier, blobUrl, errorMessageBase);
    }

    private async Task<SendMessageResponse> SendServiceBusMessage(string documentFileIdentifier, string metaDataXmlString)
    {
        var errorMessageBase = "Error occured while sending message to harvested-queue";
        try
        {
            await _serviceBusService.SendMessageAsync(metaDataXmlString);

            return new SendMessageResponse(documentFileIdentifier, true, string.Empty);
        }
        catch (ServiceBusException ex)
        {
            var errorMessage = $"{errorMessageBase}: for datasource: {_dataSourceName}, file-id: {documentFileIdentifier}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
        }

        return new SendMessageResponse(documentFileIdentifier, false, errorMessageBase);
    }


    private async Task<string> GetJnccData(string apiUrl)
    {
        try
        {
            var responseXmlString = await _apiClient.GetAsync(apiUrl);
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

    public virtual async Task<string> GetJnccMetadata(string apiUrl, string jnccFileName)
    {
        try
        {
            var responseXmlString = await _apiClient.GetAsync(apiUrl);
            return responseXmlString;
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Error occured while harvesting the metadata for Data source: {_dataSourceName}, file-id: {jnccFileName}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
            throw new DataSourceConnectionException(errorMessage, ex);
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
