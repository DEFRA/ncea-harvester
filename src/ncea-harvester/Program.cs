using Microsoft.Extensions.Configuration;
using ncea.harvester;
using ncea.harvester.infra;
using ncea.harvester.Models;
using ncea.harvester.Processors;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
IConfiguration configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory()) 
                                .AddJsonFile("appsettings.json")
                                .Build();
builder.Services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IApiClient, ApiClient>();
builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();
builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
builder.Services.AddSingleton<IJnccProcessor, JnccProcessor>();
builder.Services.AddSingleton<IMedinProcessor, MedinProcessor>();


var host = builder.Build();
host.Run();
