namespace Ncea.Harvester.Infrastructure.Models.Responses;

public class SaveBlobResponse
{
    public SaveBlobResponse(string fileIdentifier, string blobUrl, string errorMessage) =>
        (FileIdentifier, BlobUrl, ErrorMessage) = (fileIdentifier, blobUrl, errorMessage);

    public string FileIdentifier { get; set; }

    public string BlobUrl { get; set; }
    
    public string ErrorMessage { get; set; }
}
