namespace Ncea.Harvester.Infrastructure.Models.Requests;

public class BackUpContainerRequest
{
    public BackUpContainerRequest(string sourceContainer, string destinationContainer) =>
        (SourceContainer, DestinationContainer) = (sourceContainer, destinationContainer);

    public string SourceContainer { get; set; } = null!;
    public string DestinationContainer { get; set; } = null!;
}
