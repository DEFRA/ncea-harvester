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
using Ncea.Harvester.Enums;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.Extensions.Azure;
using ncea.harvester.Services.Contracts;
using ncea.harvester.Services;
using ncea.harvester.Infrastructure.Contracts;
using ncea.harvester.Infrastructure;

var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json")
                                .AddEnvironmentVariables()
                                .Build();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var dataSource = configuration.GetValue<string>("DataSource");
var dataSourceName = Enum.Parse(typeof(ProcessorType), dataSource!, true).ToString()!.ToLowerInvariant();
var processorType = (ProcessorType)Enum.Parse(typeof(ProcessorType), dataSource!, true);
var harvsesterConfigurations = configuration.GetSection("HarvesterConfigurations").Get<List<HarvesterConfiguration>>()!;

ConfigureKeyVault(configuration, builder);
ConfigureLogging(builder);
await ConfigureStorage(configuration, builder, dataSourceName);
await ConfigureServiceBusQueue(configuration, builder);
ConfigureServices(builder);
ConfigureProcessor(builder, harvsesterConfigurations, processorType);

var host = builder.Build();
host.Run();

static void ConfigureProcessor(HostApplicationBuilder builder, IList<HarvesterConfiguration> harvsesterConfigurations, ProcessorType processorType)
{
    var harvsesterConfiguration = harvsesterConfigurations.Single(x => x.ProcessorType == processorType);
    var assembly = typeof(Program).Assembly;
    var type = assembly.GetType(harvsesterConfiguration.Type);

    if (type != null)
    {
        builder.Services.AddSingleton(typeof(IProcessor), type);
        builder.Services.AddSingleton(typeof(HarvesterConfiguration), harvsesterConfiguration);
        builder.Services.AddHttpClient(harvsesterConfiguration.DataSourceApiBase).ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
        });
    }
}

static async Task ConfigureServiceBusQueue(IConfigurationRoot configuration, HostApplicationBuilder builder)
{
    var harvesterQueueName = configuration.GetValue<string>("HarvesterQueueName");
    var mapperQueueName = configuration.GetValue<string>("MapperQueueName");

    var servicebusHostName = configuration.GetValue<string>("ServiceBusHostName");

    var createQueue = configuration.GetValue<bool>("DynamicQueueCreation");
    if (createQueue)
    {
        var servicebusAdminClient = new ServiceBusAdministrationClient(servicebusHostName, new DefaultAzureCredential());
        bool harvesterQueueExists = await servicebusAdminClient.QueueExistsAsync(harvesterQueueName);
        if (!harvesterQueueExists)
        {
            await servicebusAdminClient.CreateQueueAsync(harvesterQueueName);
        }
        bool mapperQueueExists = await servicebusAdminClient.QueueExistsAsync(mapperQueueName);
        if (!mapperQueueExists)
        {
            await servicebusAdminClient.CreateQueueAsync(mapperQueueName);
        }
    }

    builder.Services.AddAzureClients(builder =>
    {
        builder.AddServiceBusClientWithNamespace(servicebusHostName);
        builder.UseCredential(new DefaultAzureCredential());
        builder.AddClient<ServiceBusSender, ServiceBusClientOptions>(
            (_, _, provider) => provider.GetService<ServiceBusClient>()!.CreateSender(harvesterQueueName))
        .WithName(harvesterQueueName);
    });
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
        loggingBuilder.AddConsole();
    });
    builder.Services.AddApplicationInsightsTelemetryWorkerService();
    builder.Services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
    {
        module.EnableSqlCommandTextInstrumentation = true;
        o.ConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");
    });
}

static async Task ConfigureStorage(IConfigurationRoot configuration, HostApplicationBuilder builder, string dataSourceName)
{
    var blobStorageEndpoint = new Uri(configuration.GetValue<string>("BlobStorageUri")!);
    var blobServiceClient = new BlobServiceClient(blobStorageEndpoint, new DefaultAzureCredential());

    builder.Services.AddSingleton(x => blobServiceClient);
    BlobContainerClient container = blobServiceClient.GetBlobContainerClient(dataSourceName);
    await container.CreateIfNotExistsAsync();

    var fileSharePath = configuration.GetValue<string>("FileShareName");
    var dirPath = Path.Combine(fileSharePath!, dataSourceName.ToLowerInvariant());
    if (!Directory.Exists(dirPath))
    {
        Directory.CreateDirectory(dirPath);
    }
}

static void ConfigureServices(HostApplicationBuilder builder)
{
    builder.Services.AddSingleton<IApiClient, ApiClient>();
    builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();
    builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
    builder.Services.AddSingleton<IBlobService, BlobService>();
    builder.Services.AddSingleton<IBlobBatchClientWrapper, BlobBatchClientWrapper>();
    builder.Services.AddSingleton<IOrchestrationService, OrchestrationService>();
    builder.Services.AddSingleton<IBackUpService, BackUpService>();
    builder.Services.AddSingleton<IDeletionService, DeletionService>();
}

[ExcludeFromCodeCoverage]
public static partial class Program { }