namespace Ncea.Harvester.Infrastructure.Contracts;

public interface IServiceBusService
{
    Task SendMessageAsync(string message);
}
