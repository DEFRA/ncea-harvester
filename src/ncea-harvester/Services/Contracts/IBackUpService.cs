namespace ncea.harvester.Services.Contracts;

public interface IBackUpService
{
    Task BackUpMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSource, CancellationToken cancellationToken);
    void BackUpEnrichedXmlFilesCreatedInPreviousRun(string dataSource);
}
