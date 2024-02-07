namespace Ncea.Harvester.Infrastructure.Models.Requests;

public class GetBlobRequest
{
    public GetBlobRequest(string blob, string container) => (Blob, Container) = (blob, container);
   
    public string Blob { get; set; }
    
    public string Container { get; set; }
}
