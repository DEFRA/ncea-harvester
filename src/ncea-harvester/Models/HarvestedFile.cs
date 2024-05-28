namespace Ncea.Harvester.Models;

public class HarvestedFile
{
    public HarvestedFile(string fileIdentifier, string blobUrl, string errorMessage, bool? hasMessageSent)
    {
        FileIdentifier = fileIdentifier;
        BlobUrl = blobUrl;
        ErrorMessage = errorMessage;
        HasMessageSent = hasMessageSent;
    }

    public string FileIdentifier { get; set; }
    public string BlobUrl { get; set; }
    public string ErrorMessage { get; set; }
    public bool? HasMessageSent { get; set; }
}
