﻿using Ncea.Harvester.Infrastructure.Contracts;
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
            startPosition = GetNextStartPostionInMedinData(out hasNextRecords, out totalRecords, responseXml);
            var metaDataXmlNodes = GetMetadataList(responseXml, hasNextRecords);
            await SendMetaDataToServiceBus(metaDataXmlNodes);

            if (startPosition != 0) hasNextRecords = (startPosition <= totalRecords);
        }
    }

    private async Task SendMetaDataToServiceBus(IEnumerable<XElement>? metaDataXmlNodes)
    {
        if (metaDataXmlNodes == null || !metaDataXmlNodes.Any()) 
          return; 
        
        foreach (var metaDataXmlNode in metaDataXmlNodes)
        {
            var documentFileIdentifier = string.Empty;

            try
            {
                documentFileIdentifier = GetFileIdentifier(metaDataXmlNode);
                string? metaDataXmlString = metaDataXmlNode.ToString();
                metaDataXmlString = string.Concat("<?xml version=\"1.0\" encoding=\"utf-8\"?>", metaDataXmlString);
                await _serviceBusService.SendMessageAsync(metaDataXmlString);

                var xmlStream = new MemoryStream(Encoding.ASCII.GetBytes(metaDataXmlString));                
                var documentFileName = string.Concat(documentFileIdentifier, ".xml");

                await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, documentFileName, _dataSourceName), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while harvesting source: {_dataSourceName}, file-id: {documentFileIdentifier}", _dataSourceName, documentFileIdentifier);
            }
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

    private async Task<XDocument> GetMedinData(int startPosition, int maxRecords)
    {
        var apiUrl = _harvesterConfiguration.DataSourceApiUrl;
        apiUrl = apiUrl.Replace("{{maxRecords}}", Convert.ToString(maxRecords)).Replace("{{startPosition}}", Convert.ToString(startPosition));
        var responseXmlString = await _apiClient.GetAsync(apiUrl);
        var responseXml = XDocument.Parse(responseXmlString);
        return responseXml;
    }
}
