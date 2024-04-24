using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Infrastructure.Models.Requests;

namespace Ncea.Harvester.Infrastructure;

public class ServiceBusService : IServiceBusService
{
    private readonly ServiceBusSender _sender;

    public ServiceBusService(IConfiguration configuration, IAzureClientFactory<ServiceBusSender> serviceBusSenderFactory)
    {
        var queueName = configuration.GetValue<string>("HarvesterQueueName");
        _sender = serviceBusSenderFactory.CreateClient(queueName);
    }

    public async Task SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken)
    {
        var messageInBytes = Encoding.UTF8.GetBytes(request.Message);
        var serviceBusMessage = new ServiceBusMessage(messageInBytes);
        serviceBusMessage.ApplicationProperties.Add("DataSource", request.DataSourceName);
        await _sender.SendMessageAsync(serviceBusMessage);
    }
}
