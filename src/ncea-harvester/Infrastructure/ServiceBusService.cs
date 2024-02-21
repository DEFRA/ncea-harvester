using System.Text;
using Azure.Messaging.ServiceBus;
using Ncea.Harvester.Infrastructure.Contracts;
using ncea.harvester.Infrastructure.Contracts;

namespace Ncea.Harvester.Infrastructure;

public class ServiceBusService : IServiceBusService
{
    private readonly ServiceBusClient _queueClient;
    private readonly string _serviceBusQueueName;

    public ServiceBusService(IHarvesterConfiguration harvesterConfiguration, ServiceBusClient queueClient)
    {
        _queueClient = queueClient;
        _serviceBusQueueName = $"{harvesterConfiguration.ProcessorType}-harvester-queue";
    }

    public async Task SendMessageAsync(string message)
    {
        var sender = _queueClient.CreateSender(_serviceBusQueueName);
        var messageInBytes = Encoding.UTF8.GetBytes(message);
        var serviceBusMessage = new ServiceBusMessage(messageInBytes);
        await sender.SendMessageAsync(serviceBusMessage);
    }
}
