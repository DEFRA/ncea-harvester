namespace Ncea.Harvester.Services.Contracts;

public interface IDeletionService
{
    Task DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSourceName, CancellationToken cancellationToken);
    void DeleteEnrichedXmlFilesCreatedInPreviousRun(string dataSourceName);
}
