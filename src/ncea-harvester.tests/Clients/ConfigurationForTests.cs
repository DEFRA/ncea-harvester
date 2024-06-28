using Microsoft.Extensions.Configuration;
using Ncea.Harvester.Enums;
using Ncea.Harvester.Models;

namespace Ncea.Harvester.Tests.Clients
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
