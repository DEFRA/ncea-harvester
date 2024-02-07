using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Ncea.Harvester.Models;
using Ncea.Harvester.Infrastructure.Contracts;

namespace Ncea.Harvester.Infrastructure;

public class ServiceBusService : IServiceBusService
{
    private readonly ServiceBusClient _queueClient;
    private readonly string _serviceBusQueueName;

    public ServiceBusService(IOptions<HarvesterConfigurations> harvesterConfigurations, ServiceBusClient queueClient)
    {
        _queueClient = queueClient;
        _serviceBusQueueName = $"{harvesterConfigurations.Value.Processor.ProcessorType}-harvester-queue";
    }

    public async Task SendMessageAsync(string messageBody)
    {
        var sender = _queueClient.CreateSender(_serviceBusQueueName);
        var messageInBytes = Encoding.UTF8.GetBytes(messageBody);
        var message = new ServiceBusMessage(messageInBytes);
        await sender.SendMessageAsync(message);
    }
}
