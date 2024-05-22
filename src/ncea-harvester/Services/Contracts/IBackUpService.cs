namespace Ncea.harvester.Services.Contracts;

public interface IBackUpService
{
    Task BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSourceName, CancellationToken cancellationToken);
    void BackUpEnrichedXmlFilesCreatedInPreviousRun(string dataSourceName);
}
