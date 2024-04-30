using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Ncea.Harvester.Infrastructure.Contracts;
using ncea.harvester.Services.Contracts;
using ncea.harvester.Services;
using FluentAssertions;

namespace ncea_harvester.tests.Processors;

public class BackUpServiceTests
{
    private readonly IBackUpService _backupService;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILogger<BackUpService>> _loggerMock;

    public BackUpServiceTests()
    {
        _blobServiceMock = new Mock<IBlobService>();
        _loggerMock = new Mock<ILogger<BackUpService>>();

        List<KeyValuePair<string, string?>> lstProps =
            [
                new KeyValuePair<string, string?>("FileShareName", Directory.GetCurrentDirectory()),
            ];

        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(lstProps)
                            .Build();
        _backupService = new BackUpService(configuration, _blobServiceMock.Object, _loggerMock.Object);
    }

    public void BackUpEnrichedXmlFilesCreatedInPreviousRun_()
    {
        //Arrange
        var dataSourceName = "test-datasource";
        var dataSourceDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), dataSourceName);

        Directory.CreateDirectory(dataSourceDirectoryPath);
        File.WriteAllText(Path.Combine(dataSourceDirectoryPath, "enriched-xml-1.xml"), "test-content-1");
        File.WriteAllText(Path.Combine(dataSourceDirectoryPath, "enriched-xml-2.xml"), "test-content-2");

        var backupDirectoryName = $"{dataSourceName}_backup";
        var backupDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), backupDirectoryName);
        
        //Act
        _backupService.BackUpEnrichedXmlFilesCreatedInPreviousRun(dataSourceName);

        //Assert
        Directory.Exists(dataSourceDirectoryPath).Should().BeTrue();
        Directory.Exists(backupDirectoryPath).Should().BeTrue();
        Directory.GetFiles(backupDirectoryPath).Count().Should().Be(2);
        Directory.GetFiles(dataSourceDirectoryPath).Count().Should().Be(0);
    }
}
