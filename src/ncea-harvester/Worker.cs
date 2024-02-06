using Microsoft.Extensions.Options;
using ncea.harvester.Models;
using ncea.harvester.Processors;

namespace ncea.harvester
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IProcessor _processor;
        private readonly IHostApplicationLifetime _lifetime;

        public Worker(ILogger<Worker> logger, IOptions<AppSettings> appSettings, IProcessor processor, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _appSettings = appSettings;
            _processor = processor;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                await _processor.Process();
                _lifetime.StopApplication();
            }
        }
    }
}
