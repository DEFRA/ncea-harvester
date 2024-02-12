using Microsoft.Extensions.Options;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;

namespace Ncea.Harvester.Processors;

public class MedinProcessor : IProcessor
{
    private readonly IApiClient _apiClient;
    private readonly IServiceBusService _serviceBusService;
    private readonly IBlobService _blobService;
    private readonly ILogger<MedinProcessor> _logger;
    private readonly HarvesterConfigurations _harvesterConfigurations;

    public MedinProcessor(IApiClient apiClient,
        IServiceBusService serviceBusService,
        IBlobService blobService,
        ILogger<MedinProcessor> logger,
        IOptions<HarvesterConfigurations> harvesterConfigurations)
    {
        _apiClient = apiClient;
        _harvesterConfigurations = harvesterConfigurations.Value;
        _apiClient.CreateClient(_harvesterConfigurations.Processor.DataSourceApiBase);
        _serviceBusService = serviceBusService;
        _logger = logger;
        _blobService = blobService;
    }
    public Task Process()
    {
        Console.Write(_harvesterConfigurations);
        Console.Write(_apiClient);
        Console.Write(_serviceBusService);
        Console.Write(_blobService);
        Console.Write(_logger);
        Console.Write(_apiClient);
        throw new NotImplementedException();
    }
}
