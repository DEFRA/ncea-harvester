﻿using Microsoft.Extensions.Options;
using ncea.harvester.infra;
using ncea.harvester.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ncea.harvester.Processors
{
    public class MedinProcessor : IProcessor
    {
        private readonly IApiClient _apiClient;
        private readonly IServiceBusService _serviceBusService;
        private readonly AppSettings _appSettings;

        public MedinProcessor(IApiClient apiClient, IServiceBusService serviceBusService, IOptions<AppSettings> appSettings)
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
}
