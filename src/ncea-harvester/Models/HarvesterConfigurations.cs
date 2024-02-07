using Ncea.Harvester.Constants;

namespace Ncea.Harvester.Models
{
    public class HarvesterConfigurations
    {
        public Processor Processor { get; set; } = null!;
        public string ServiceBusConnectionString { get; set; } = null!;
        public string ServiceBusQueueName { get; set; } = null!;
        public string KeyVaultUri { get; set; } = null!;
    }

    public class Processor
    {
        public ProcessorType ProcessorType { get; set; }
        public string Type { get; set; } = null!;
        public string DataSourceApiBase { get; set; } = null!;
        public string DataSourceApiUrl { get; set; } = null!;
        public string Schedule { get; set; } = null!;
    }
}

