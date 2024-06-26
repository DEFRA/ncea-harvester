using System.Xml.Linq;

namespace Ncea.Mapper.Services.Contracts;

public interface IValidationService
{
    bool IsValid(XElement harvestedDataElement);
}
