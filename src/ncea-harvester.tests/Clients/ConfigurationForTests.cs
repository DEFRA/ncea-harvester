using Microsoft.Extensions.Configuration;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ncea_harvester.tests.Clients
{
    public static class ConfigurationForTests
    {
        public static HarvesterConfiguration GetHarvesterConfiguration(ProcessorType processorType)
        {
            var configuration = GetConfiguration();
            var harvsesterConfigurations = configuration.GetSection("HarvesterConfigurations").Get<List<HarvesterConfiguration>>()!;
            var harvesterConfiguration = harvsesterConfigurations.FirstOrDefault(x => x.ProcessorType == processorType);
            return harvesterConfiguration!;
        }

        public static IConfigurationRoot GetConfiguration()
        {
            IConfigurationRoot configuration;
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var applicationExeDirectory = Path.GetDirectoryName(location);
            var builder = new ConfigurationBuilder()
                .SetBasePath(applicationExeDirectory!)
                .AddJsonFile("appsettings.json");
            configuration = builder.Build();
            return configuration;
        }
    }
}
