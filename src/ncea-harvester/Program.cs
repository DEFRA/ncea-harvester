using Azure.Identity;
using Azure.Storage.Blobs;
using Ncea.Harvester;
using Ncea.Harvester.Infrastructure;
using Azure.Messaging.ServiceBus;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using Azure.Security.KeyVault.Secrets;

var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json")
                                .Build();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var keyVaultEndpoint = new Uri(configuration.GetValue<string>("AppSettings:KeyVaultUri"));
var blobStorageEndpoint = new Uri(configuration.GetValue<string>("AppSettings:BlobStorage"));
var servicebusHostName = configuration.GetValue<string>("AppSettings:ServiceBusHostName");

builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddSingleton(x => new SecretClient(keyVaultEndpoint, new DefaultAzureCredential()));
builder.Services.AddSingleton(x => new BlobServiceClient(blobStorageEndpoint, new DefaultAzureCredential()));
builder.Services.AddSingleton(x => new ServiceBusClient(servicebusHostName, new DefaultAzureCredential()));

builder.Services.Configure<HarvesterConfigurations>(configuration.GetSection("AppSettings"));
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IApiClient, ApiClient>();
builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();
builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
builder.Services.AddSingleton<IBlobService, BlobService>();
ConfigureProcessor(builder, configuration);

var host = builder.Build();
host.Run();

static void ConfigureProcessor(HostApplicationBuilder builder, IConfiguration configuration)
{
    var processorTypeName = configuration.GetValue<string>("AppSettings:Processor:Type");
    var assembly = typeof(Program).Assembly;
    var type = assembly.GetType(processorTypeName);

    if (type != null)
    {
        builder.Services.AddSingleton(typeof(IProcessor), type);
    }
}