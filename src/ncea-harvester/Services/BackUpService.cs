using ncea.harvester.Services.Contracts;
using Ncea.Harvester.Infrastructure.Contracts;

namespace ncea.harvester.Services;

public class BackUpService : IBackUpService
{
    private readonly string _fileSharePath;
    private readonly IBlobService _blobService;
    private readonly ILogger _logger;

    public BackUpService(IConfiguration configuration, IBlobService blobService, ILogger<BackUpService> logger)
    {
        _fileSharePath = configuration.GetValue<string>("FileShareName")!;
        _blobService = blobService;
        _logger = logger;
    }

    public void BackUpEnrichedXmlFilesCreatedInPreviousRun(string dataSource)
    {
        var dirPath = Path.Combine(_fileSharePath, dataSource);
        var backupDirPath = Path.Combine(_fileSharePath, $"{dataSource}_backup");

        var backupResult = RenameFolder(dirPath, backupDirPath);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
    }

    public Task BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSource, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Renames a folder name
    /// </summary>
    /// <param name="directory">The full directory of the folder</param>
    /// <param name="newFolderName">New name of the folder</param>
    /// <returns>Returns true if rename is successfull</returns>
    private static bool RenameFolder(string directory, string newFolderName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directory) ||
                string.IsNullOrWhiteSpace(newFolderName))
            {
                return false;
            }

            var oldDirectory = new DirectoryInfo(directory);

            if (!oldDirectory.Exists)
            {
                return false;
            }

            if (string.Equals(oldDirectory.Name, newFolderName, StringComparison.OrdinalIgnoreCase))
            {
                //new folder name is the same with the old one.
                return false;
            }

            string newDirectory;

            if (oldDirectory.Parent == null)
            {
                //root directory
                newDirectory = Path.Combine(directory, newFolderName);
            }
            else
            {
                newDirectory = Path.Combine(oldDirectory.Parent.FullName, newFolderName);
            }

            if (Directory.Exists(newDirectory))
            {
                //target directory already exists
                return false;
            }

            oldDirectory.MoveTo(newDirectory);

            return true;
        }
        catch
        {
            //ignored
            return false;
        }
    }
}
