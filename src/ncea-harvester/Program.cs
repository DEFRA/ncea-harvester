using Azure.Identity;
using ncea.harvester;
using ncea.harvester.infra;
using ncea.harvester.Models;
using ncea.harvester.Processors;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddApplicationInsightsTelemetryWorkerService();

var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json")
                                .Build();

var keyVaultEndpoint = new Uri(configuration.GetValue<string>("KeyVaultUri"));
builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

builder.Services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IApiClient, ApiClient>();
builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();
builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
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