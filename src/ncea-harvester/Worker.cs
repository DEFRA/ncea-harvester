using Cronos;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;

namespace Ncea.Harvester;

public class Worker : BackgroundService
{
    private readonly CronExpression _cron;
    private readonly ILogger<Worker> _logger;
    private TelemetryClient _telemetryClient;
    private readonly IOptions<HarvesterConfigurations> _appSettings;
    private readonly IProcessor _processor;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, IOptions<HarvesterConfigurations> appSettings, IProcessor processor, IHostApplicationLifetime lifetime, TelemetryClient telemetryClient)
    {
        _logger = logger;
        _appSettings = appSettings;
        _processor = processor;
        _lifetime = lifetime;
        _cron = CronExpression.Parse(appSettings.Value.Processor.Schedule);
        _telemetryClient = telemetryClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
            {
                _logger.LogInformation("Metadata harversting started");
                await _processor.Process();
                _logger.LogInformation("Metadata harversting completed");
                _telemetryClient.TrackEvent("Harvesting completed");
            }

            var utcNow = DateTime.UtcNow;
            var nextUtc = _cron.GetNextOccurrence(utcNow);
            await Task.Delay(nextUtc.Value - utcNow, stoppingToken);
        }
    }
}
