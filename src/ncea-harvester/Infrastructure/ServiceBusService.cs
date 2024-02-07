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
    private readonly HarvesterConfigurations _appSettings;

    public ServiceBusService(IOptions<HarvesterConfigurations> appSettings, ServiceBusClient queueClient)
    {
        _appSettings = appSettings.Value;
        _queueClient = queueClient;
        _serviceBusQueueName = _appSettings.ServiceBusQueueName;
    }

    public async Task SendMessageAsync(string messageBody)
    {
        var sender = _queueClient.CreateSender(_serviceBusQueueName);
        var messageInBytes = Encoding.UTF8.GetBytes(messageBody);
        var message = new ServiceBusMessage(messageInBytes);
        await sender.SendMessageAsync(message);
    }
}
