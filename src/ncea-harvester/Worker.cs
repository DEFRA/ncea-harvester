using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using Ncea.Harvester.Utils;
using System.Diagnostics.CodeAnalysis;

namespace Ncea.Harvester;

[ExcludeFromCodeCoverage]
public class Worker : BackgroundService
{    
    private readonly ILogger _logger;
    private readonly IProcessor _processor;
    private readonly TelemetryClient _telemetryClient;
    private readonly HarvesterConfiguration _harvesterConfiguration;    
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public Worker(HarvesterConfiguration harvesterConfiguration, 
        IProcessor processor, 
        TelemetryClient telemetryClient,         
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger)
    {        
        _harvesterConfiguration = harvesterConfiguration;
        _processor = processor;
        _telemetryClient = telemetryClient;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ncea Metadata Harvesting started at: {time}", DateTimeOffset.Now);

        using (_telemetryClient.StartOperation<RequestTelemetry>("harvester-operation"))
        {
            _logger.LogInformation("Metadata harversting started for {source}", _harvesterConfiguration.ProcessorType);

            try
            {
                await _processor.ProcessAsync(stoppingToken);
            }
            catch(Exception Ex)
            {
                var errorMessage = $"Error occurred while harvesting metadata from {_harvesterConfiguration.ProcessorType}";
                CustomLogger.LogErrorMessage(_logger, errorMessage, Ex);
            }
            finally
            {
                _logger.LogInformation("Metadata harversting ended for {source}", _harvesterConfiguration.ProcessorType);
                _telemetryClient.TrackEvent("Harvesting completed");

                await _telemetryClient.FlushAsync(stoppingToken);
                _hostApplicationLifetime.StopApplication();
            }
        }

        _logger.LogInformation("Ncea Metadata Harvesting ended at: {time}", DateTimeOffset.Now);
    }
}
