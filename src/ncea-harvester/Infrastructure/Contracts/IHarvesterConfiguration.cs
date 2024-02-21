using Ncea.Harvester.Constants;

namespace ncea.harvester.Infrastructure.Contracts;

public interface IHarvesterConfiguration
{
    public ProcessorType ProcessorType { get; set; }
    public string Type { get; set; }
    public string DataSourceApiBase { get; set; }
    public string DataSourceApiUrl { get; set; }
    public string Schedule { get; set; }
}
