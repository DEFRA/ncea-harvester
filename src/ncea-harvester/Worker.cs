using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Ncea.Harvester;

[ExcludeFromCodeCoverage]
public class Worker : BackgroundService
{    
    private readonly ILogger _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly HarvesterConfiguration _harvesterConfiguration;
    private readonly IProcessor _processor;

    public Worker(ILogger<Worker> logger, HarvesterConfiguration harvesterConfiguration, IProcessor processor, TelemetryClient telemetryClient)
    {
        _logger = logger;
        _harvesterConfiguration = harvesterConfiguration;
        _processor = processor;
        _telemetryClient = telemetryClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
        {
            _logger.LogInformation("Metadata harversting started for {source}", _harvesterConfiguration.ProcessorType);
            await _processor.Process();
            _logger.LogInformation("Metadata harversting completed");
            _telemetryClient.TrackEvent("Harvesting completed");
        }
    }
}
