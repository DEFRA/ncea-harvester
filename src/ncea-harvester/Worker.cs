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
        Console.WriteLine("Inside Worker");
        _logger.LogInformation("Ncea Metadata Harvesting started at: {time}", DateTimeOffset.Now);

        using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
        {
            _logger.LogInformation("Metadata harversting started for {source}", _harvesterConfiguration.ProcessorType);

            try
            {
                await _processor.Process();
            }
            catch(Exception Ex)
            {
                Console.WriteLine("Error occured while harvesting metadata from {source}", _harvesterConfiguration.ProcessorType);
                _logger.LogError(Ex, "Error occured while harvesting metadata from {source}", _harvesterConfiguration.ProcessorType);
            }
            finally
            {
                _logger.LogInformation("Metadata harversting completed");
                _telemetryClient.TrackEvent("Harvesting completed");

                _hostApplicationLifetime.StopApplication();
            }
        }

        _logger.LogInformation("Ncea Metadata Harvesting ended at: {time}", DateTimeOffset.Now);
    }
}
