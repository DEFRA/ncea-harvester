using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using ncea.harvester.Services;
using Ncea.Harvester.Infrastructure.Contracts;
using Azure;

namespace Ncea.Harvester.Tests.Processors;

public class DeletionServiceTests
{
    private readonly DeletionService _deletionService;
    private readonly IConfiguration _configuration;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILogger<DeletionService>> _loggerMock;

    public DeletionServiceTests()
    {
        _blobServiceMock = new Mock<IBlobService>();
        _loggerMock = new Mock<ILogger<DeletionService>>();

        _blobServiceMock.Setup(x => x.DeleteBlobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _loggerMock.Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            )
        );

        List<KeyValuePair<string, string?>> lstProps =
            [                
                new KeyValuePair<string, string?>("FileShareName", Directory.GetCurrentDirectory()),
            ];

        _configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(lstProps)
                            .Build();
        _deletionService = new DeletionService(_configuration, _blobServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void DeleteEnrichedXmlFilesCreatedInPreviousRun_WhenNoFilesExists_DeleteTheBackupFolder()
    {
        //Arrange
        var dataSourceName = "test-datasource-3";
        var backupDirectoryName = $"{dataSourceName}-backup";
        var backupDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), backupDirectoryName);        
        if (Directory.Exists(backupDirectoryPath))
        {
            Directory.Delete(backupDirectoryPath, true);
        }

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
        var dataSourceName = "test-datasource-4";
        var backupDirectoryName = $"{dataSourceName}-backup";
        var backupDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), backupDirectoryName);
        if (Directory.Exists(backupDirectoryPath))
        {
            Directory.Delete(backupDirectoryPath, true);
        }

        Directory.CreateDirectory(backupDirectoryPath);
        File.WriteAllText(Path.Combine(backupDirectoryPath, "enriched-xml-1.xml"), "test-content-1");
        File.WriteAllText(Path.Combine(backupDirectoryPath, "enriched-xml-2.xml"), "test-content-2");

        //Act
        _deletionService.DeleteEnrichedXmlFilesCreatedInPreviousRun(dataSourceName);

        //Assert
        var result = Directory.Exists(backupDirectoryPath);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMetadataXmlBlobsCreatedInPreviousRunAsync_WhenExecutingSucessfully_CallDeleteBlobsAsync()
    {
        //Arrange
        var dataSourceName = "test-datasource";
        var backupConatinerName = $"{dataSourceName}-backup";

        //Act
        await _deletionService.DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(dataSourceName, CancellationToken.None);

        //Assert
        _blobServiceMock.Verify(x => x.DeleteBlobsAsync(backupConatinerName, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeleteMetadataXmlBlobsCreatedInPreviousRunAsync_WhenThrowingException_TheLogTheError()
    {
        //Arrange
        var dataSourceName = "test-datasource";
        var backupConatinerName = $"{dataSourceName}-backup";

        _blobServiceMock.Setup(x => x.DeleteBlobsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Status: 404 (The specified blob does not exist.)"));

        var deletionService = new DeletionService(_configuration, _blobServiceMock.Object, _loggerMock.Object);

        //Act
        await _deletionService.DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(dataSourceName, CancellationToken.None);

        //Assert
        _blobServiceMock.Verify(x => x.DeleteBlobsAsync(backupConatinerName, CancellationToken.None), Times.Once);
        _loggerMock.Verify(
            m => m.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(1),
            It.IsAny<string>()
        );
    }
}
