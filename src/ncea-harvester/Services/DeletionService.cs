using ncea.harvester.Services.Contracts;
using Ncea.Harvester.Infrastructure.Contracts;

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

    public void DeleteEnrichedXmlFilesCreatedInPreviousRun(string dataSource)
    {
        var backupDirPath = Path.Combine(_fileSharePath, $"{dataSource}_backup");
        if (Directory.Exists(backupDirPath))
        {
            Directory.Delete(backupDirPath, true);
        }
    }

    public async Task DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSource, CancellationToken cancellationToken)
    {
        await _blobService.DeleteBlobsAsync(dataSource, cancellationToken);
    }
}
