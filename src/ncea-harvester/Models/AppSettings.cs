using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ncea.harvester.Constants;

namespace ncea.harvester.Models
{
    public class AppSettings
    {
        public Processor Processor { get; set; }
        public string ServiceBusConnectionString { get; set; }
        public string ServiceBusQueueName { get; set; }
        public string KeyVaultUri { get; set; }
    }

    public class Processor
    {
        public ProcessorType ProcessorType { get; set; }
        public string Type { get; set; }
        public string DataSourceApiBase { get; set; }
        public string DataSourceApiUrl { get; set; }
    }
}

