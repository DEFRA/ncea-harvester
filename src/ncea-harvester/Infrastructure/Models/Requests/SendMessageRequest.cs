namespace Ncea.Harvester.Infrastructure.Models.Requests;

public class SendMessageRequest
{
    public SendMessageRequest(string message) =>
        (Message) = (message);

    public string Message { get; set; }
}
