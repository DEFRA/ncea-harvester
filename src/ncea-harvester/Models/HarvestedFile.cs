
namespace Ncea.Harvester.Models;

public class HarvestedFile
{
    public HarvestedFile(string fileIdentifier, string blobUrl, string fileContent, string errorMessage)
    {
        FileIdentifier = fileIdentifier;
        BlobUrl = blobUrl;
        FileContent = fileContent;
        ErrorMessage = errorMessage;
    }
    public string FileIdentifier { get; set; }
    public string BlobUrl { get; set; }
    public string FileContent { get; set; }
    public string ErrorMessage { get; set; }
}
