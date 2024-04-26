namespace ncea.harvester.Services.Contracts;

public interface IDeletionService
{
    Task DeleteMetadataXmlBlobsCreatedInPreviousRunAsync(string dataSource, CancellationToken cancellationToken);
    void DeleteEnrichedXmlFilesCreatedInPreviousRun(string dataSource);
}
