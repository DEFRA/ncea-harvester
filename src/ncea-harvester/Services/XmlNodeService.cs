using Ncea.Harvester.Services.Contracts;
using Ncea.Harvester.Models;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Ncea.Harvester.Services;

public class XmlNodeService : IXmlNodeService
{
    private const string GmdNamespace = "http://www.isotc211.org/2005/gmd";
    private const string GcoNamespace = "http://www.isotc211.org/2005/gco";
    private const string GmxNamespace = "http://www.isotc211.org/2005/gmx";

    private readonly string _mdcSchemaLocationPath;

    public XmlNodeService(IConfiguration configuration)
    {
        _mdcSchemaLocationPath = configuration.GetValue<string>("MdcSchemaLocation")!;
    }

    public string GetNodeValues(MandatoryField field, XElement rootNode, XmlNamespaceManager nsMgr)
    {
        var value = string.Empty;

        if (field.Type == "list")
        {
            var elements = rootNode.XPathSelectElements(field.Xpath, nsMgr);
            if (elements != null && elements.Any())
            {
                var values = elements.Select(x => x.Value).ToList();
                return string.Join(", ", values);
            }
        }
        else
        {
            var element = rootNode.XPathSelectElement(field.Xpath, nsMgr);
            return element != null ? element.Value : string.Empty;
        }

        return value;
    }

    public XmlNamespaceManager GetXmlNamespaceManager(XElement xElement)
    {
        var reader = xElement.CreateReader();
        XmlNamespaceManager nsMgr = new XmlNamespaceManager(reader.NameTable);
        nsMgr.AddNamespace("gmd", GmdNamespace);
        nsMgr.AddNamespace("gco", GcoNamespace);
        nsMgr.AddNamespace("gmx", GmxNamespace);
        nsMgr.AddNamespace("mdc", _mdcSchemaLocationPath);

        return nsMgr;
    }
}
