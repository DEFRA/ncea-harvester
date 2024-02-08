using Azure.Identity;
using Azure.Storage.Blobs;
using Ncea.Harvester;
using Ncea.Harvester.Infrastructure;
using Azure.Messaging.ServiceBus;
using Ncea.Harvester.Infrastructure.Contracts;
using Ncea.Harvester.Models;
using Ncea.Harvester.Processors.Contracts;
using Azure.Security.KeyVault.Secrets;
using Azure.Messaging.ServiceBus.Administration;
using Ncea.Harvester.Constants;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.ApplicationInsights;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.ApplicationInsights.DependencyCollector;

var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json")
                                .AddEnvironmentVariables()
                                .Build();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<HarvesterConfigurations>(configuration.GetSection("HarvesterConfigurations"));
builder.Services.AddHttpClient();

var processorType = configuration.GetValue<string>("HarvesterConfigurations:Processor:ProcessorType");
var dataSourceName = Enum.Parse(typeof(ProcessorType), processorType!).ToString()!.ToLowerInvariant();

ConfigureKeyVault(configuration, builder);
ConfigureLogging(builder);
await ConfigureBlobStorage(configuration, builder, dataSourceName);
await ConfigureServiceBusQueue(configuration, builder, dataSourceName);
ConfigureServices(builder);
ConfigureProcessor(builder, configuration);

var host = builder.Build();
host.Run();

static void ConfigureProcessor(HostApplicationBuilder builder, IConfiguration configuration)
{
    var processorTypeName = configuration.GetValue<string>("HarvesterConfigurations:Processor:Type");
    var assembly = typeof(Program).Assembly;
    var type = assembly.GetType(processorTypeName!);

    if (type != null)
    {
        builder.Services.AddSingleton(typeof(IProcessor), type);
    }
}

static async Task ConfigureServiceBusQueue(IConfigurationRoot configuration, HostApplicationBuilder builder, string dataSourceName)
{
    var servicebusHostName = configuration.GetValue<string>("ServiceBusHostName");
    builder.Services.AddSingleton(x => new ServiceBusClient(servicebusHostName, new DefaultAzureCredential()));    
    
    var queueName = $"{dataSourceName}-harvester-queue";

    var servicebusAdminClient = new ServiceBusAdministrationClient(servicebusHostName, new DefaultAzureCredential());
    bool queueExists = await servicebusAdminClient.QueueExistsAsync(queueName);
    if (!queueExists)
    {
        await servicebusAdminClient.CreateQueueAsync(queueName);
    }
}

static void ConfigureKeyVault(IConfigurationRoot configuration, HostApplicationBuilder builder)
{
    var keyVaultEndpoint = new Uri(configuration.GetValue<string>("KeyVaultUri")!);
    builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
    builder.Services.AddSingleton(x => new SecretClient(keyVaultEndpoint, new DefaultAzureCredential()));
}

static void ConfigureLogging(HostApplicationBuilder builder)
{
    builder.Services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddApplicationInsights(
            configureTelemetryConfiguration: (config) =>
                config.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString"),
                configureApplicationInsightsLoggerOptions: (options) => { }
            );
        loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>(null, LogLevel.Information);

    });
    builder.Services.AddApplicationInsightsTelemetryWorkerService();
    builder.Services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
    {
        module.EnableSqlCommandTextInstrumentation = true;
        o.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");
    });
}

static async Task ConfigureBlobStorage(IConfigurationRoot configuration, HostApplicationBuilder builder, string dataSourceName)
{
    var blobStorageEndpoint = new Uri(configuration.GetValue<string>("BlobStorageUri")!);
    var blobServiceClient = new BlobServiceClient(blobStorageEndpoint, new DefaultAzureCredential());

    builder.Services.AddSingleton(x => blobServiceClient);
    BlobContainerClient container = blobServiceClient.GetBlobContainerClient(dataSourceName);
    await container.CreateIfNotExistsAsync();
}

static void ConfigureServices(HostApplicationBuilder builder)
{
    builder.Services.AddSingleton<IApiClient, ApiClient>();
    builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();
    builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
    builder.Services.AddSingleton<IBlobService, BlobService>();
}



[ExcludeFromCodeCoverage]
public partial class Program { }