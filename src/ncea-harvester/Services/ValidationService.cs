using Ncea.Harvester.Models;
using Ncea.Harvester.Services.Contracts;
using System.Xml.Linq;

namespace Ncea.Harvester.Services;

public class ValidationService : IValidationService
{
    private readonly HarvesterConfiguration _harvesterConfiguration;
    private readonly IXmlNodeService _xmlNodeService;

    public ValidationService(HarvesterConfiguration harvesterConfiguration, IXmlNodeService xmlNodeService)
    {
        _harvesterConfiguration = harvesterConfiguration;
        _xmlNodeService = xmlNodeService;
    }

    public bool IsValid(XElement harvestedDataElement)
    {

        var harvesterXmlStr = Convert.ToString(harvestedDataElement);
        var harvesterXmlDoc = XDocument.Parse(harvesterXmlStr!);
        var harvesterXmlRoot = harvesterXmlDoc.Root!;

        var nmr = _xmlNodeService.GetXmlNamespaceManager(harvesterXmlRoot);

        foreach(var field in _harvesterConfiguration.MandatoryFields)
        {
            var fieldValue = _xmlNodeService.GetNodeValues(field, harvesterXmlRoot, nmr);

            if (string.IsNullOrWhiteSpace(fieldValue))
                return false;
        }

        return true;
    }
}
