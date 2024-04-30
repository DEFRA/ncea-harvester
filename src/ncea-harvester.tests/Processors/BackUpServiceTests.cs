using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Ncea.Harvester.Infrastructure.Contracts;
using ncea.harvester.Services.Contracts;
using ncea.harvester.Services;
using FluentAssertions;
using Azure;
using Ncea.Harvester.Infrastructure.Models.Requests;

namespace Ncea.Harvester.Tests.Processors;

public class BackUpServiceTests
{
    private readonly IBackUpService _backupService;
    private readonly IConfiguration _configuration;
    private readonly Mock<IBlobService> _blobServiceMock;
    private readonly Mock<ILogger<BackUpService>> _loggerMock;

    public BackUpServiceTests()
    {
        _blobServiceMock = new Mock<IBlobService>();
        _loggerMock = new Mock<ILogger<BackUpService>>();

        _blobServiceMock.Setup(x => x.BackUpContainerAsync(It.IsAny<BackUpContainerRequest>(), It.IsAny<CancellationToken>()))
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
        _backupService = new BackUpService(_configuration, _blobServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void BackUpEnrichedXmlFilesCreatedInPreviousRun_WhenNoFilesExists_RenameTheDataSourceFolderAndCreateNewdataSourceFolder()
    {
        //Arrange
        var dataSourceName = "test-datasource-1";
        var dataSourceDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), dataSourceName);
        var backupDirectoryName = $"{dataSourceName}_backup";
        var backupDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), backupDirectoryName);

        if (Directory.Exists(dataSourceDirectoryPath))
        {
            Directory.Delete(dataSourceDirectoryPath, true);
        }
        if (Directory.Exists(backupDirectoryPath))
        {
            Directory.Delete(backupDirectoryPath, true);
        }

        Directory.CreateDirectory(dataSourceDirectoryPath);

        //Act
        _backupService.BackUpEnrichedXmlFilesCreatedInPreviousRun(dataSourceName);

        //Assert
        Directory.Exists(dataSourceDirectoryPath).Should().BeTrue();
        Directory.Exists(backupDirectoryPath).Should().BeTrue();
        Directory.GetFiles(backupDirectoryPath).Count().Should().Be(0);
        Directory.GetFiles(dataSourceDirectoryPath).Count().Should().Be(0);
    }

    [Fact]
    public void BackUpEnrichedXmlFilesCreatedInPreviousRun_WhenFilesExists_RenameTheDataSourceFolderAndCreateNewdataSourceFolder()
    {
        //Arrange
        var dataSourceName = "test-datasource-2";
        var dataSourceDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), dataSourceName);
        var backupDirectoryName = $"{dataSourceName}_backup";
        var backupDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), backupDirectoryName);

        if (Directory.Exists(dataSourceDirectoryPath))
        {
            Directory.Delete(dataSourceDirectoryPath, true);
        }
        if(Directory.Exists(backupDirectoryPath)) 
        {
            Directory.Delete(backupDirectoryPath, true);
        }

        Directory.CreateDirectory(dataSourceDirectoryPath);
        File.WriteAllText(Path.Combine(dataSourceDirectoryPath, "enriched-xml-1.xml"), "test-content-1");
        File.WriteAllText(Path.Combine(dataSourceDirectoryPath, "enriched-xml-2.xml"), "test-content-2");
        
        //Act
        _backupService.BackUpEnrichedXmlFilesCreatedInPreviousRun(dataSourceName);

        //Assert
        Directory.Exists(dataSourceDirectoryPath).Should().BeTrue();
        Directory.Exists(backupDirectoryPath).Should().BeTrue();
        Directory.GetFiles(backupDirectoryPath).Count().Should().Be(2);
        Directory.GetFiles(dataSourceDirectoryPath).Count().Should().Be(0);
    }

    [Fact]
    public async Task BackUpMetadataXmlBlobsCreatedInPreviousRunAsync_WhenExecutingSucessfully_CallDeleteBlobsAsync()
    {
        //Arrange
        var dataSourceName = "test-datasource";
        var backupDirectoryName = $"{dataSourceName}_backup";

        //Act
        await _backupService.BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(dataSourceName, CancellationToken.None);

        //Assert
        _blobServiceMock.Verify(x => x.BackUpContainerAsync(It.IsAny<BackUpContainerRequest>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task BackUpMetadataXmlBlobsCreatedInPreviousRunAsync_WhenThrowingException_TheLogTheError()
    {
        //Arrange
        var dataSourceName = "test-datasource";
        var backupDirectoryName = $"{dataSourceName}_backup";

        _blobServiceMock.Setup(x => x.BackUpContainerAsync(It.IsAny<BackUpContainerRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Status: 404 (The specified blob does not exist.)"));

        var deletionService = new BackUpService(_configuration, _blobServiceMock.Object, _loggerMock.Object);

        //Act
        await _backupService.BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(dataSourceName, CancellationToken.None);

        //Assert
        _blobServiceMock.Verify(x => x.BackUpContainerAsync(It.IsAny<BackUpContainerRequest>(), CancellationToken.None), Times.Once);
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
