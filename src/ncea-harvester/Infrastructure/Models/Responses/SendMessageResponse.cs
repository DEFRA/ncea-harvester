
namespace Ncea.Harvester.Infrastructure.Models.Responses;

public class SendMessageResponse
{
    public SendMessageResponse(string fileIdentifier, bool isSucceeded, string errorMessage) =>
        (FileIdentifier, IsSucceeded, ErrorMessage) = (fileIdentifier, isSucceeded, errorMessage);

    public string FileIdentifier { get; set; }

    public bool IsSucceeded { get; set; }

    public string ErrorMessage { get; set; }
}
