﻿using Azure;
using Azure.Messaging.ServiceBus;
using HtmlAgilityPack;
using ncea.harvester.BusinessExceptions;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
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

        foreach (var documentLink in documentLinks)
        {
            var apiUrl = "/waf/" + documentLink;
            var metaDataXmlString = await GetJnccMetadata(apiUrl, documentLink);
            var documentFileIdentifier = GetFileIdentifier(metaDataXmlString);

            try
            {
                if (!string.IsNullOrWhiteSpace(documentFileIdentifier))
                {
                    await SendServiceBusMessage(documentFileIdentifier, metaDataXmlString);
                    await SaveMetadataXml(documentFileIdentifier, metaDataXmlString);
                }
                else
                {
                    _logger.LogError("File Identifier missing");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while harvesting source: {_dataSourceName}, file-id: {documentFileIdentifier}", _dataSourceName, documentFileIdentifier);
            }
        }
    }

    private async Task<string> GetJnccData(string apiUrl)
    {
        try
        {
            var responseXmlString = await _apiClient.GetAsync(apiUrl);
            return responseXmlString;
        }
        catch (DataSourceConnectionException ex)
        {
            _logger.LogError(ex, "Error occured while harvesting the metadata for Data source: {_dataSourceName}", _dataSourceName);
            throw;
        }
    }

    private async Task<string> GetJnccMetadata(string apiUrl, string jnccFileName)
    {
        try
        {
            var responseXmlString = await _apiClient.GetAsync(apiUrl);
            return responseXmlString;
        }
        catch (HttpRequestException ex)
        {
            var errorMessage = $"Error occured while harvesting the metadata for Data source: {_dataSourceName}";
            _logger.LogError(ex, errorMessage, _dataSourceName);
            throw new DataSourceConnectionException(errorMessage, ex);
        }
        catch (TaskCanceledException ex)
        {
            string? errorMessage;
            if (ex.CancellationToken.IsCancellationRequested)
            {
                errorMessage = $"Request was cancelled while harvesting the metadata for Data source: {_dataSourceName}";
                _logger.LogError(ex, errorMessage, _dataSourceName);
            }
            else
            {
                errorMessage = $"Request timed out while harvesting the metadata for Data source: {_dataSourceName}";
                _logger.LogError(ex, errorMessage, _dataSourceName);
            }

            throw new DataSourceConnectionException(errorMessage, ex);
        }
    }

    private async Task SendServiceBusMessage(string documentFileIdentifier, string metaDataXmlString)
    {
        try
        {
            await _serviceBusService.SendMessageAsync(metaDataXmlString);
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "Error occured while sending message to harvested-queue for Data source: {_dataSourceName}, file-id: {documentFileIdentifier}", _dataSourceName, documentFileIdentifier);
        }
    }

    private async Task SaveMetadataXml(string? documentFileIdentifier, string metaDataXmlString)
    {
        var xmlStream = new MemoryStream(Encoding.ASCII.GetBytes(metaDataXmlString));
        var documentFileName = string.Concat(documentFileIdentifier, ".xml");

        try
        {
            await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, documentFileName, _dataSourceName), CancellationToken.None);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error occured while saving the file to the blob storage for Data source: {_dataSourceName}, file-id: {documentFileIdentifier}", _dataSourceName, documentFileIdentifier);

        }
    }

    private static List<string> GetDocumentLinks(string responseHtmlString)
    {
        var documentLinks = new List<string>();
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(responseHtmlString);

        var anchorNodes = htmlDocument?.DocumentNode?.SelectNodes("//a[@href]");
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
