
namespace Ncea.Harvester.Infrastructure.Models.Requests;

public class SendMessageRequest
{
    public SendMessageRequest(string dataSourceName, string fileIdentifier, string message) =>
        (DataSourceName, FileIdentifier, Message) = (dataSourceName, fileIdentifier, message);

    public string DataSourceName { get; set; }

    public string FileIdentifier { get; set; }

    public string Message { get; set; }
}
