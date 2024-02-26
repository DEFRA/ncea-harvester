# Welcome to the NCEA Harvester Repository

This is the code repository for the NCEA Metadata Harvester Microservice codebase.

# Prerequisites

Before proceeding, ensure you have the following installed:

- .NET 8 SDK: You can download and install it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0).

# Configuration

1. **Processor Configuration**
   
   Below explains the properties of Processor configuration.

    ***ProcessorType:***
    This config is defined for the type of processor to be injected while running the Harvester service.
    Possible values are defined in "Constants.ProcessorType" enum.
   
    Example: 
    `Jncc`
    `Medin`

    ***Type:***
    This will be the Class Type of the processor.
   
    Example:
    `Ncea.Harvester.Processors.JnccProcessor`
    `Ncea.Harvester.Processors.MedinProcessor`

    ***DataSourceApiBase:***
    This configuration is for setting the data source API base URI.
   
    Example: 
    `https://data.jncc.gov.uk`

    ***DataSourceApiUrl:***
    Data source API url can be configured with the endpoint from which data can be pulled.
   
    Example:  For Jncc data source DataSourceApiUrl will be `"/waf/index.html"`

    ***Schedule:***
    We can configure the schedule of the harvester configuration with this setting.
3. **Cloud Service Configuration**
   
    ***ServiceBus Configuration***
    We are using ServiceBusHostName to connect to ServiceBus service.
   
    Example:
    `"ServiceBusHostName": "harvestersb.servicebus.windows.net"`

    ***KeyVault Configuration***
    KeyVaultUri expects the KeyVault Uri to connect to the KayVault service.
   
    Example:
    `"KeyVaultUri": "https://nceakv.vault.azure.net"`

    ***BlobStorage Configuration***
    BlobStorageUri config is used for connecting to Blob Storage.
   
    Example:
    `"BlobStorageUri": "https://nceaharvesterblob.blob.core.windows.net"`

    ***ApplicationInsights Configuration***
    We are using ServiceBusHostName to connect to ServiceBus service.
   
    Example:
    `"ApplicationInsights": {
        "LogLevel": {
        "Default": "Information"
        }
    }`

    ***Pipeline Variables***
    Variable Groups
    - pipelineVariables
        - *acrConatinerRegistry*
        - *acrContainerRepositoryHarvester*
        - *acrName*
        - *sonarCloudOrganization*
        - *sonarProjectKeyHarvester*
        - *sonarProjectNameHarvester*
    - azureVariables-[dev/test/sandbox/...]
        - *aksNamespace*
        - *blobStorageUri*
        - *keyVaultUri*
        - *serviceBusHostName*
    - *harvesterServiceVariables-[dev/test/sandbox/...]*
        - *containerRepostitoryFullPath*
        - *jnccSchedule*
        - *medinSchedule*
        - *serviceAccountHarvester*