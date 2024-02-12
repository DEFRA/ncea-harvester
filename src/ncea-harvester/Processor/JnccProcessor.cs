﻿using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using System.Text;

namespace Ncea.Harvester.Processors;

public class JnccProcessor : IProcessor
{
    private readonly IApiClient _apiClient;
    private readonly IServiceBusService _serviceBusService;
    private readonly IBlobService _blobService;
    private readonly ILogger _logger;
    private readonly HarvesterConfigurations _harvesterConfigurations;

    public JnccProcessor(IApiClient apiClient, 
        IServiceBusService serviceBusService, 
        IBlobService blobService,
        ILogger<JnccProcessor> logger,
        IOptions<HarvesterConfigurations> harvesterConfigurations)
    {
        _apiClient = apiClient;
        _harvesterConfigurations = harvesterConfigurations.Value;
        _apiClient.CreateClient(_harvesterConfigurations.Processor.DataSourceApiBase);
        _serviceBusService = serviceBusService;
        _logger = logger;
        _blobService = blobService;
    }
    public async Task Process()
    {
        var responseHtmlString = await _apiClient.GetAsync(_harvesterConfigurations.Processor.DataSourceApiUrl);
        var documentLinks = GetDocumentLinks(responseHtmlString);
        await SendMetaDataToServiceBus(documentLinks);
    }

    private async Task SendMetaDataToServiceBus(List<string> documentLinks)
    {
        foreach (var documentLink in documentLinks)
        {
            try
            {
                var apiUrl = "/waf/" + documentLink;
                var metaDataXmlString = await _apiClient.GetAsync(apiUrl);
                await _serviceBusService.SendMessageAsync(metaDataXmlString);
                var xmlStream = new MemoryStream(Encoding.ASCII.GetBytes(metaDataXmlString));
                var dataSourceName = _harvesterConfigurations.Processor.ProcessorType.ToString().ToLowerInvariant();
                await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, Path.GetFileName(documentLink), dataSourceName), CancellationToken.None);                
            } catch (Exception ex)
            {
                _logger.LogError($"Error occured {ex.StackTrace}");
                Console.WriteLine(ex);
            }
        }
    }

    private static List<string> GetDocumentLinks(string responseHtmlString)
    {
        var documentLinks = new List<string>();
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(responseHtmlString);

        var anchorNodes = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
        if (anchorNodes != null)
        {
            foreach (var anchorNode in anchorNodes)
            {
                var hrefValue = anchorNode.GetAttributeValue("href", "");
                documentLinks.Add("/" + hrefValue);
            }
        }
        return documentLinks;
    }
}