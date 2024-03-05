using HtmlAgilityPack;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
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
        var responseHtmlString = await _apiClient.GetAsync(_harvesterConfiguration.DataSourceApiUrl);
        var documentLinks = GetDocumentLinks(responseHtmlString);
        await SendMetaDataToServiceBus(documentLinks);
    }

    private async Task SendMetaDataToServiceBus(List<string> documentLinks)
    {
        foreach (var documentLink in documentLinks)
        {
            var documentFileIdentifier = string.Empty;
            try
            {
                var apiUrl = "/waf/" + documentLink;
                var metaDataXmlString = await _apiClient.GetAsync(apiUrl);
                await _serviceBusService.SendMessageAsync(metaDataXmlString);

                var xmlStream = new MemoryStream(Encoding.ASCII.GetBytes(metaDataXmlString));
                documentFileIdentifier = GetFileIdentifier(metaDataXmlString);
                var documentFileName = string.Concat(documentFileIdentifier, ".xml");

                await _blobService.SaveAsync(new SaveBlobRequest(xmlStream, documentFileName, _dataSourceName), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occured while harvesting source: {_dataSourceName}, file-id: {documentFileIdentifier}", _dataSourceName, documentFileIdentifier);
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
                documentLinks.Add(hrefValue);
            }
        }
        return documentLinks;
    }

    private static string? GetFileIdentifier(string xmlString)
    {
        //Xml string to XElement Conversion
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
