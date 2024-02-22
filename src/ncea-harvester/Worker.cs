using Cronos;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace Ncea.Harvester;

[ExcludeFromCodeCoverage]
public class Worker : BackgroundService
{
    private readonly CronExpression _cron;
    private readonly ILogger _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly HarvesterConfiguration _harvesterConfiguration;
    private readonly IProcessor _processor;

    public Worker(ILogger<Worker> logger, HarvesterConfiguration harvesterConfiguration, IProcessor processor, TelemetryClient telemetryClient)
    {
        _logger = logger;
        _harvesterConfiguration = harvesterConfiguration;
        _processor = processor;
        _cron = CronExpression.Parse(_harvesterConfiguration.Schedule);
        _telemetryClient = telemetryClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                _logger.LogInformation("Metadata harversting started for {source}", _harvesterConfiguration.ProcessorType);
                await _processor.Process();
                _logger.LogInformation("Metadata harversting completed");
                _telemetryClient.TrackEvent("Harvesting completed");
            }

            //var utcNow = DateTime.UtcNow;
            //var nextUtc = _cron.GetNextOccurrence(utcNow);
            //nextUtc = (nextUtc == null ? utcNow : nextUtc);
            //await Task.Delay(nextUtc.Value - utcNow, stoppingToken);
        }
    }
}
