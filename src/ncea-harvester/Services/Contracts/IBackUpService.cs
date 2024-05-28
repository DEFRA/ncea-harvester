namespace Ncea.Harvester.Services.Contracts;

public interface IBackUpService
{
    Task BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSourceName, CancellationToken cancellationToken);
    void BackUpEnrichedXmlFilesCreatedInPreviousRun(string dataSourceName);
}
