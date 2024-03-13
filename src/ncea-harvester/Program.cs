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
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.Extensions.Azure;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json")
                                .AddEnvironmentVariables()
                                .Build();

var builder = Host.CreateApplicationBuilder(args);
var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
    config.AddConfiguration(builder.Configuration.GetSection("Logging"));
}).CreateLogger("Program");

builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient();

var dataSource = configuration.GetValue<string>("DataSource");
var dataSourceName = Enum.Parse(typeof(ProcessorType), dataSource!, true).ToString()!.ToLowerInvariant();
var processorType = (ProcessorType)Enum.Parse(typeof(ProcessorType), dataSource!, true);
var harvsesterConfigurations = configuration.GetSection("HarvesterConfigurations").Get<List<HarvesterConfiguration>>()!;

logger.LogInformation("Configure KeyVault");
ConfigureKeyVault(configuration, builder);
logger.LogInformation("Configure Logging");
ConfigureLogging(builder);
logger.LogInformation("Configure BlobStorage");
await ConfigureBlobStorage(configuration, builder, dataSourceName);
logger.LogInformation("Configure Servicebus Queue");
await ConfigureServiceBusQueue(configuration, builder);
logger.LogInformation("Configure Services");
ConfigureServices(builder);
logger.LogInformation("Configure Processor");
ConfigureProcessor(builder, harvsesterConfigurations, processorType);

//logger.LogInformation("Bing access test...");
//var httpClient1 = new HttpClient();
//var res1 = await httpClient1.GetAsync("https://bing.com");
//logger.LogInformation("Calling bing completed with status:" + res1.StatusCode);

//logger.LogInformation("Medin access test...");
//var httpClient2 = new HttpClient();
//var res2 = await httpClient2.GetAsync("https://portal.medin.org.uk");
//logger.LogInformation("Calling Medin completed with status:" + res2.StatusCode);

//logger.LogInformation("Jncc access test...");
//var httpClient3 = new HttpClient();
//var res3 = await httpClient3.GetAsync("https://data.jncc.gov.uk");
//logger.LogInformation("Calling Jncc completed with status:" + res3.StatusCode);

logger.LogInformation("Build Host");
var host = builder.Build();
logger.LogInformation("Host Run");
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
public static partial class Program { }