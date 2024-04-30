﻿using Azure;
using ncea.harvester.Services.Contracts;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;
using Ncea.Harvester.Utils;

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
        var backupDirName = $"{dataSource}_backup";

        try
        {
            RenameFolder(dirPath, backupDirName);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error occured while performing backup operation for datasource: {dataSource}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
        }

       
    }

    public async Task BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSource, CancellationToken cancellationToken)
    {
        var backupConatinerName = $"{dataSource}_backup";
        try
        {
            await _blobService.BackUpContainerAsync(new BackUpContainerRequest(dataSource, backupConatinerName), cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            var errorMessage = $"Error occured while performing backup operation for datasource: {dataSource}";
            CustomLogger.LogErrorMessage(_logger, errorMessage, ex);
        }
    }

    /// <summary>
    /// Renames a folder name
    /// </summary>
    /// <param name="dataSourceEnrichedXmlDirectoryPath">The full directory of the folder</param>
    /// <param name="newFolderName">New name of the folder</param>
    /// <returns>Returns true if rename is successfull</returns>
    private static void RenameFolder(string dataSourceEnrichedXmlDirectoryPath, string newBackUpFolderName)
    {
        if (string.IsNullOrWhiteSpace(dataSourceEnrichedXmlDirectoryPath) 
            || string.IsNullOrWhiteSpace(newBackUpFolderName))
        {
            throw new ArgumentException($"One of the given argument is not valid - dataSourceEnrichedXmlDirectoryPath : {dataSourceEnrichedXmlDirectoryPath}, newBackUpFolderName : {newBackUpFolderName}");
        }

        var oldDirectory = new DirectoryInfo(dataSourceEnrichedXmlDirectoryPath);

        if (!oldDirectory.Exists)
        {
            throw new DirectoryNotFoundException($"Given datasouce directory not found {dataSourceEnrichedXmlDirectoryPath}");
        }

        string newDirectory;

        if (oldDirectory.Parent == null)
        {
            //root directory
            newDirectory = Path.Combine(dataSourceEnrichedXmlDirectoryPath, newBackUpFolderName);
        }
        else
        {
            newDirectory = Path.Combine(oldDirectory.Parent.FullName, newBackUpFolderName);
        }
        oldDirectory.MoveTo(newDirectory);
    }
}