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
        var nmr = _xmlNodeService.GetXmlNamespaceManager(harvestedDataElement);

        foreach(var field in _harvesterConfiguration.MandatoryFields)
        {
            var fieldValue = _xmlNodeService.GetNodeValues(field, harvestedDataElement, nmr);

            if (string.IsNullOrWhiteSpace(fieldValue))
                return false;
        }

        return true;
    }
}
