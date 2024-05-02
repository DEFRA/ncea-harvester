using Azure;
using ncea.harvester.Services.Contracts;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Utils;

namespace ncea.harvester.Services;

public class DeletionService : IDeletionService
{
    private readonly string _fileSharePath;
    private readonly IBlobService _blobService;
    private readonly ILogger _logger;

    public DeletionService(IConfiguration configuration, IBlobService blobService, ILogger<DeletionService> logger)
    {
        _fileSharePath = configuration.GetValue<string>("FileShareName")!;
        _blobService = blobService;
        _logger = logger;
    }

    public void DeleteEnrichedXmlFilesCreatedInPreviousRun(string dataSourceName)
    {
        var backupDirPath = Path.Combine(_fileSharePath, $"{dataSourceName}-backup");
        if (Directory.Exists(backupDirPath))
        {
            Directory.Delete(backupDirPath, true);
        }
    }

    public async Task DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSourceName, CancellationToken cancellationToken)
    {
        var backupConatinerName = $"{dataSourceName}-backup";
        try
        {
            await _blobService.DeleteBlobsAsync(backupConatinerName, cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            var errorMessage = $"Error occured while performing cleanup operation for datasource: {dataSourceName}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
        }
    }
}
