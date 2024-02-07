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

var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json")
                                .AddEnvironmentVariables()
                                .Build();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<HarvesterConfigurations>(configuration.GetSection("AppSettings"));
builder.Services.AddHttpClient();

ConfigureKeyVault(configuration, builder);
ConfigiureLogging(builder);
ConfigureBlobStorage(configuration, builder);
await ConfigureServiceBusQueue(configuration, builder);
ConfigureServices(builder);
ConfigureProcessor(builder, configuration);

var host = builder.Build();
host.Run();

static void ConfigureProcessor(HostApplicationBuilder builder, IConfiguration configuration)
{
    var processorTypeName = configuration.GetValue<string>("AppSettings:Processor:Type");
    var assembly = typeof(Program).Assembly;
    var type = assembly.GetType(processorTypeName!);

    if (type != null)
    {
        builder.Services.AddSingleton(typeof(IProcessor), type);
    }
}

static async Task ConfigureServiceBusQueue(IConfigurationRoot configuration, HostApplicationBuilder builder)
{
    var servicebusHostName = configuration.GetValue<string>("ServiceBusHostName");
    builder.Services.AddSingleton(x => new ServiceBusClient(servicebusHostName, new DefaultAzureCredential()));

    var sourceName = configuration.GetValue<string>("AppSettings:Processor:ProcessorType");
    var processorType = Enum.Parse(typeof(ProcessorType), sourceName!);
    var queueName = $"{processorType}-harvester-queue";

    var servicebusAdminClient = new ServiceBusAdministrationClient(servicebusHostName, new DefaultAzureCredential());
    bool queueExists = await servicebusAdminClient.QueueExistsAsync(queueName);
    if (!queueExists)
    {
        await servicebusAdminClient.CreateQueueAsync(queueName);
    }
}

static void ConfigureKeyVault(IConfigurationRoot configuration, HostApplicationBuilder builder)
{
    var keyVaultEndpoint = new Uri(configuration.GetValue<string>("KeyVaultUri"));
    builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
    builder.Services.AddSingleton(x => new SecretClient(keyVaultEndpoint, new DefaultAzureCredential()));
}

static void ConfigiureLogging(HostApplicationBuilder builder)
{
    builder.Services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddApplicationInsights();
    });
    builder.Services.AddApplicationInsightsTelemetryWorkerService();
}

static void ConfigureBlobStorage(IConfigurationRoot configuration, HostApplicationBuilder builder)
{
    var blobStorageEndpoint = new Uri(configuration.GetValue<string>("BlobStorageUri"));
    builder.Services.AddSingleton(x => new BlobServiceClient(blobStorageEndpoint, new DefaultAzureCredential()));
}

static void ConfigureServices(HostApplicationBuilder builder)
{
    builder.Services.AddSingleton<IApiClient, ApiClient>();
    builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();
    builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
    builder.Services.AddSingleton<IBlobService, BlobService>();
}