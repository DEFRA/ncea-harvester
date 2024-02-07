using HtmlAgilityPack;
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
    private readonly HarvesterConfigurations _appSettings;

    public JnccProcessor(IApiClient apiClient, IServiceBusService serviceBusService, IBlobService blobService, IOptions<HarvesterConfigurations> appSettings)
    {
        _apiClient = apiClient;
        _appSettings = appSettings.Value;
        _apiClient.CreateClient(_appSettings.Processor.DataSourceApiBase);
        _serviceBusService = serviceBusService;
        _blobService = blobService;
    }
    public async Task Process()
    {
        var responseHtmlString = await _apiClient.GetAsync(_appSettings.Processor.DataSourceApiUrl);
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
                await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, "jncc", Path.GetFileName(documentLink)), CancellationToken.None);                
            } catch (Exception ex)
            {
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
