using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ncea.harvester.Services;
using ncea.harvester.Services.Contracts;
using Ncea.Harvester.Infrastructure.Contracts;

namespace Ncea.Harvester.Tests.Processors;

public class DeletionServiceTests
{
    private readonly IDeletionService _deletionService;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILogger<DeletionService>> _loggerMock;

    public DeletionServiceTests()
    {
        _blobServiceMock = new Mock<IBlobService>();
        _loggerMock = new Mock<ILogger<DeletionService>>();

        List<KeyValuePair<string, string?>> lstProps =
            [                
                new KeyValuePair<string, string?>("FileShareName", Directory.GetCurrentDirectory()),
            ];

        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(lstProps)
                            .Build();
        _deletionService = new DeletionService(configuration, _blobServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void DeleteEnrichedXmlFilesCreatedInPreviousRun_WhenNoFilesExists_DeleteTheBackupFolder()
    {
        //Arrange
        var dataSourceName = "test-datasource";
        var backupDirectoryName = $"{dataSourceName}_backup";
        var backupDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), backupDirectoryName);
        Directory.CreateDirectory(backupDirectoryPath);

        //Act
        _deletionService.DeleteEnrichedXmlFilesCreatedInPreviousRun(dataSourceName);

        //Assert
        var result = Directory.Exists(backupDirectoryPath);
        result.Should().BeFalse();
    }

    
    [Fact]
    public void DeleteEnrichedXmlFilesCreatedInPreviousRun_WhenFilesExists_DeleteTheBackupFolder()
    {
        //Arrange
        var dataSourceName = "test-datasource";
        var backupDirectoryName = $"{dataSourceName}_backup";
        var backupDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), backupDirectoryName);
        Directory.CreateDirectory(backupDirectoryPath);
        File.WriteAllText(Path.Combine(backupDirectoryPath, "enriched-xml-1.xml"), "test-content-1");
        File.WriteAllText(Path.Combine(backupDirectoryPath, "enriched-xml-2.xml"), "test-content-2");

        //Act
        _deletionService.DeleteEnrichedXmlFilesCreatedInPreviousRun(dataSourceName);

        //Assert
        var result = Directory.Exists(backupDirectoryPath);
        result.Should().BeFalse();
    }
}
