using System.Xml.Linq;

namespace Ncea.Harvester.Services.Contracts;

public interface IValidationService
{
    bool IsValid(XElement harvestedDataElement);
}
