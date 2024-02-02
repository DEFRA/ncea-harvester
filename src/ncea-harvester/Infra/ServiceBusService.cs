using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ncea.harvester.Models;
using Azure.Identity;

namespace ncea.harvester.infra
{
    public interface IServiceBusService
    {
        Task SendMessageAsync(string message);
    }
    public class ServiceBusService : IServiceBusService
    {
        private readonly ServiceBusClient _queueClient;
        private readonly string _serviceBusConnectionString;
        private readonly string _serviceBusQueueName;
        private readonly AppSettings _appSettings;

        public ServiceBusService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            _serviceBusConnectionString = _appSettings.ServiceBusConnectionString;
            _serviceBusQueueName = _appSettings.ServiceBusQueueName;
            _queueClient = new ServiceBusClient(_appSettings.ServiceBusConnectionString, new DefaultAzureCredential());
        }

        public async Task SendMessageAsync(string messageBody)
        {
            var sender = _queueClient.CreateSender(_serviceBusQueueName);
            var messageInBytes = Encoding.UTF8.GetBytes(messageBody);
            var message = new ServiceBusMessage(messageInBytes);
            await sender.SendMessageAsync(message);
        }
    }
}
