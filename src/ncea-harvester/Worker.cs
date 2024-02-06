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

        public Worker(ILogger<Worker> logger, IOptions<AppSettings> appSettings, IProcessor processor)
        {
            _logger = logger;
            _appSettings = appSettings;
            _processor = processor;
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

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
