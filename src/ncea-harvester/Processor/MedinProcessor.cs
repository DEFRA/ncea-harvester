using Microsoft.Extensions.Options;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;

namespace Ncea.Harvester.Processors;

public class MedinProcessor : IProcessor
{
    private readonly IApiClient _apiClient;
    private readonly IServiceBusService _serviceBusService;
    private readonly HarvesterConfigurations _appSettings;

    public MedinProcessor(IApiClient apiClient, IServiceBusService serviceBusService, IOptions<HarvesterConfigurations> appSettings)
    {
        _apiClient = apiClient;
        _appSettings = appSettings.Value;
        _apiClient.CreateClient(_appSettings.Processor.DataSourceApiBase);
    }
    public Task Process()
    {
        throw new NotImplementedException();
    }
}
