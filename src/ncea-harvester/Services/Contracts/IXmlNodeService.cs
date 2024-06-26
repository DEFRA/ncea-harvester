using Ncea.Harvester.Models;
using System.Xml;
using System.Xml.Linq;

namespace Ncea.Mapper.Services.Contracts;

public interface IXmlNodeService
{
    XmlNamespaceManager GetXmlNamespaceManager(XElement xDoc);    
    string GetNodeValues(MandatoryField field, XElement rootNode, XmlNamespaceManager nsMgr);
}
