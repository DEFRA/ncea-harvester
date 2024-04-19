using Azure;
using Azure.Messaging.ServiceBus;
using ncea.harvester.BusinessExceptions;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using System.Text;
using System.Xml.Linq;

namespace Ncea.Harvester.Processors;

public class MedinProcessor : IProcessor
{
    private readonly string _dataSourceName;
    private readonly IApiClient _apiClient;
    private readonly IServiceBusService _serviceBusService;
    private readonly IBlobService _blobService;
    private readonly ILogger<MedinProcessor> _logger;
    private readonly HarvesterConfiguration _harvesterConfiguration;

    public MedinProcessor(IApiClient apiClient,
        IServiceBusService serviceBusService,
        IBlobService blobService,
        ILogger<MedinProcessor> logger,
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
        var startPosition = 1;
        var maxRecords = 100;
        var totalRecords = 0;
        var hasNextRecords = true;

        while (hasNextRecords)
        {
            var responseXml = await GetMedinData(startPosition, maxRecords);
            startPosition = GetNextStartPostionInMedinData(out hasNextRecords, out totalRecords, responseXml!);
            var metaDataXmlNodes = GetMetadataList(responseXml, hasNextRecords);

            if (metaDataXmlNodes != null)
            {
                foreach (var metaDataXmlNode in metaDataXmlNodes)
                {
                    var documentFileIdentifier = GetFileIdentifier(metaDataXmlNode);

                    if (!string.IsNullOrWhiteSpace(documentFileIdentifier))
                    {
                        string metaDataXmlString = await SendServiceBusMessage(documentFileIdentifier, metaDataXmlNode);
                        await SaveMetadataXml(documentFileIdentifier, metaDataXmlString);
                    }
                    else
                    {
                        _logger.LogError("File Identifier missing");
                    }
                }
            }            

            if (startPosition != 0) hasNextRecords = (startPosition <= totalRecords);
        }
    }

    private async Task<XDocument?> GetMedinData(int startPosition, int maxRecords)
    {
        var apiUrl = _harvesterConfiguration.DataSourceApiUrl;
        apiUrl = apiUrl.Replace("{{maxRecords}}", Convert.ToString(maxRecords)).Replace("{{startPosition}}", Convert.ToString(startPosition));

        XDocument? responseDocument;
        try
        {
            var responseXmlString = await _apiClient.GetAsync(apiUrl);
            responseDocument = XDocument.Parse(responseXmlString);
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = "Error occured while harvesting the metadata for Data source: {0}, start position: {startPosition}";
#pragma warning disable CA2254 // Template should be a static expression
            _logger.LogError(ex, errorMessage, _dataSourceName, startPosition);
#pragma warning restore CA2254 // Template should be a static expression
            throw new DataSourceConnectionException(errorMessage, ex);
        }
        catch (TaskCanceledException ex)
        {
            string? errorMessage;
            if (ex.CancellationToken.IsCancellationRequested)
            {
                errorMessage = "Request was cancelled while harvesting the metadata for Data source: {_dataSourceName}, start position: {startPosition}";
                #pragma warning disable CA2254 // Template should be a static expression
            _logger.LogError(ex, errorMessage, _dataSourceName, startPosition);
#pragma warning restore CA2254 // Template should be a static expression

            }
            else
            {
                errorMessage = "Request timed out while harvesting the metadata for Data source: {_dataSourceName}, start position: {startPosition}";
#pragma warning disable CA2254 // Template should be a static expression
                _logger.LogError(ex, errorMessage, _dataSourceName, startPosition);
#pragma warning restore CA2254 // Template should be a static expression

            }

            throw new DataSourceConnectionException(errorMessage, ex);
        }
        return responseDocument;
    }

    private async Task<string> SendServiceBusMessage(string documentFileIdentifier, XElement metaDataXmlNode)
    {
        string? metaDataXmlString = metaDataXmlNode.ToString();
        metaDataXmlString = string.Concat("<?xml version=\"1.0\" encoding=\"utf-8\"?>", metaDataXmlString);
        try
        {
            await _serviceBusService.SendMessageAsync(metaDataXmlString);
        }
        catch(ServiceBusException ex)
        {
            _logger.LogError(ex, "Error occured while sending message to harvested-queue for Data source: {_dataSourceName}, file-id: {documentFileIdentifier}", _dataSourceName, documentFileIdentifier);
        }        
        return metaDataXmlString;
    }

    private async Task SaveMetadataXml(string? documentFileIdentifier, string metaDataXmlString)
    {
        var xmlStream = new MemoryStream(Encoding.ASCII.GetBytes(metaDataXmlString));
        var documentFileName = string.Concat(documentFileIdentifier, ".xml");

        try
        {
            await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, documentFileName, _dataSourceName), CancellationToken.None);
        }
        catch(RequestFailedException ex) 
        {
            _logger.LogError(ex, "Error occured while saving the file to the blob storage for Data source: {_dataSourceName}, file-id: {documentFileIdentifier}", _dataSourceName, documentFileIdentifier);
        }
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
