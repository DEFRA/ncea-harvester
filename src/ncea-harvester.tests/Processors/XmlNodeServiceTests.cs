using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ncea.Harvester.Services;
using Ncea.Harvester.Models;
using Ncea.Harvester.Tests.Clients;
using System.Xml;
using System.Xml.Linq;

namespace Ncea.Enricher.Tests.Services;

public class XmlNodeServiceTests
{
    private IServiceProvider _serviceProvider;
    private XDocument _xDoc;
    private XmlNamespaceManager _xmlNamespaceManager;

    public XmlNodeServiceTests()
    {
        var filePath = GetFilePath("MEDIN_Metadata_dataset_no_fileidentifier_XmlNodeSrviceTests.xml");
        _xDoc = XDocument.Load(filePath);

        _serviceProvider = ServiceProviderForTests.Get();
        _xmlNamespaceManager = GetXmlNamespaceManager();
    }

    [Fact]
    public void GetNodeValues_WhenTextValueExists_ReturnTextValue()
    {
        // Arrange
        var configuration = _serviceProvider.GetService<IConfiguration>();
        var xmlNodeService = new XmlNodeService(configuration!);
        var field = new MandatoryField
        {
            Name = "Title",
            Type = "text",
            Xpath = "//gmd:identificationInfo/gmd:MD_DataIdentification/gmd:citation/gmd:CI_Citation/gmd:title/gco:CharacterString"
        };
        

        // Act
        var result = xmlNodeService.GetNodeValues(field, _xDoc.Root!, _xmlNamespaceManager);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void GetNodeValues_WhenTextValueNotExists_ReturnEmptyString()
    {
        // Arrange
        var configuration = _serviceProvider.GetService<IConfiguration>();
        var xmlNodeService = new XmlNodeService(configuration!);
        var field = new MandatoryField
        {
            Name = "FileIdentifier",
            Type = "text",
            Xpath = "//gmd:fileIdentifier/gco:CharacterString"
        };

        // Act
        var result = xmlNodeService.GetNodeValues(field, _xDoc.Root!, _xmlNamespaceManager);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNodeValues_WhenListValueExists_ReturnCommaSeperatedTextValues()
    {
        // Arrange
        var configuration = _serviceProvider.GetService<IConfiguration>();
        var xmlNodeService = new XmlNodeService(configuration!);
        var field = new MandatoryField
        {
            Name = "PointOfContact",
            Type = "list",
            Xpath = "//gmd:identificationInfo/*/gmd:pointOfContact/gmd:CI_ResponsibleParty/gmd:organisationName/gco:CharacterString"
        };

        // Act
        var result = xmlNodeService.GetNodeValues(field, _xDoc.Root!, _xmlNamespaceManager);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();
        result.Should().NotBeEmpty();
        result.Should().Contain(",");
    }

    [Fact]
    public void GetNodeValues_WhenListValueNotExists_ReturnEmptyString()
    {
        // Arrange
        var filePath = GetFilePath("MEDIN_Metadata_dataset_no_pointofcontact_XmlNodeSrviceTests.xml");
        var _xDocNoPointOfContact = XDocument.Load(filePath);
        var configuration = _serviceProvider.GetService<IConfiguration>();
        var xmlNodeService = new XmlNodeService(configuration!);
        var field = new MandatoryField
        {
            Name = "PointOfContact",
            Type = "list",
            Xpath = "//gmd:identificationInfo/*/gmd:pointOfContact/gmd:CI_ResponsibleParty/gmd:organisationName/gco:CharacterString"
        };

        // Act
        var result = xmlNodeService.GetNodeValues(field, _xDocNoPointOfContact.Root!, _xmlNamespaceManager);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetXmlNamespaceManager_ReturnsNamespaceManager()
    {
        // Arrange
        var configuration = _serviceProvider.GetService<IConfiguration>();
        var xmlNodeService = new XmlNodeService(configuration!);        

        // Act
        var result = xmlNodeService.GetXmlNamespaceManager(_xDoc.Root!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<XmlNamespaceManager>();
        result.HasNamespace("gmd").Should().BeTrue();
        result.HasNamespace("gco").Should().BeTrue();
        result.HasNamespace("gmx").Should().BeTrue();
        result.HasNamespace("mdc").Should().BeTrue();
    }


    private XmlNamespaceManager GetXmlNamespaceManager()
    {
        var reader = _xDoc.CreateReader();
        XmlNamespaceManager nsMgr = new XmlNamespaceManager(reader.NameTable);
        nsMgr.AddNamespace("gmd", "http://www.isotc211.org/2005/gmd");
        nsMgr.AddNamespace("gco", "http://www.isotc211.org/2005/gco");
        nsMgr.AddNamespace("gmx", "http://www.isotc211.org/2005/gmx");
        nsMgr.AddNamespace("mdc", "https://github.com/DEFRA/ncea-geonetwork/tree/main/core-geonetwork/schemas/iso19139/src/main/plugin/iso19139/schema2007/mdc");

        return nsMgr;
    }

    private static string GetFilePath(string fileName)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", fileName);
        return filePath;
    }
}
