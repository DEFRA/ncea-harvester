using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;

namespace Ncea.Harvester.Infrastructure;

public class ServiceBusService : IServiceBusService
{
    private readonly ServiceBusSender _sender;
    private readonly HarvesterConfiguration _harvesterConfiguration;

    public ServiceBusService(HarvesterConfiguration harvesterConfiguration, IConfiguration configuration, IAzureClientFactory<ServiceBusSender> serviceBusSenderFactory)
    {
        var queueName = configuration.GetValue<string>("HarvesterQueueName");
        _sender = serviceBusSenderFactory.CreateClient(queueName);
        _harvesterConfiguration = harvesterConfiguration;
    }

    public async Task SendMessageAsync(string message)
    {
        var messageInBytes = Encoding.UTF8.GetBytes(message);
        var serviceBusMessage = new ServiceBusMessage(messageInBytes);
        serviceBusMessage.ApplicationProperties.Add("DataSource", _harvesterConfiguration.ProcessorType.ToString());
        await _sender.SendMessageAsync(serviceBusMessage);
    }
}
