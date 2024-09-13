# Welcome to the NCEA Harvester Repository

This is the code repository for the NCEA Metadata Harvester Microservice codebase.

# Prerequisites

Before proceeding, ensure you have the following installed:

- .NET 8 SDK: You can download and install it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0).

# Configurations

## Processor Configurations
   
Below explains the properties of Processor configuration.

***ProcessorType:***
    This config is defined for the type of processor to be injected while running the Harvester service.
    Possible values are defined in *Enums.DataSource* enum.
   
    Example: Jncc | Medin 

***Type:***
    This will be the Class Type of the processor.
   
    Example: Ncea.Harvester.Processors.JnccProcessor | Ncea.Harvester.Processors.MedinProcessor

***DataSourceApiBase:***
    To provide DataSource base uri path.
   
    Example: https://data.jncc.gov.uk | https://portal.medin.org.uk

***DataSourceApiUrl:***
    To provide Data source API Endpoint.
   
    Example:  /waf/index.html | /geonetwork?SERVICE=CSW&VERSION=2.0.2&REQUEST=GetRecords&outputFormat=application/xml&resultType=results&ElementSetName=full&outputSchema=http://www.isotc211.org/2005/gmd&maxRecords={{maxRecords}}&startPosition={{startPosition}}

***MandatoryFields:***
    To provide mandatory field configurations.

    "MandatoryFields": [
        {
          "Name": "FileIdentifier",
          "Type": "text",
          "Xpath": "//gmd:fileIdentifier/gco:CharacterString"
        },
        {
          "Name": "Title",
          "Type": "text",
          "Xpath": "//gmd:identificationInfo/*/gmd:citation/gmd:CI_Citation/gmd:title/gco:CharacterString"
        },
        {
          "Name": "Abstract",
          "Type": "text",
          "Xpath": "//gmd:identificationInfo/*/gmd:abstract/gco:CharacterString"
        },
        {
          "Name": "PointOfContact",
          "Type": "list",
          "Xpath": "//gmd:CI_ResponsibleParty[./gmd:organisationName/gco:CharacterString != '' and (./gmd:contactInfo/gmd:CI_Contact/gmd:address/gmd:CI_Address/gmd:electronicMailAddress/* != '' or ./gmd:role/gmd:CI_RoleCode != '')]/gmd:organisationName"
        }
      ]


## Azure Dependencies
   
***ServiceBus Configurations:***
    *ServiceBusHostName* to connect to ServiceBus, to send messages in servicebus queues and to dynamically create queues, if the *DynamicQueueCreation* is set to *True*   

    "ServiceBusHostName": "DEVNCESBINF1401.servicebus.windows.net"
    "HarvesterQueueName": "harvested-queue"
    "MapperQueueName": "mapped-queue",
    "DynamicQueueCreation": true,

***KeyVault Configurations:***
    *KeyVaultUri* to access Azure KeyVault and to access secrets and connection strings.   

    "KeyVaultUri": "https://devnceinfkvt1401.vault.azure.net/"

***BlobStorage Configuration:***
    *BlobStorageUri* to connect to Azure Blob Storage, to create containers per DataSource and to Save the XML files for the respective data source.
       
    "BlobStorageUri": "https://devnceinfst1401.blob.core.windows.net"

***FileShare Configuration:***
    *FileShareName* to connect Azure File Share where Enriched XML Files are saved.   

    "FileShareName": "/metadata-import"

***ApplicationInsights Configuration:***
    *ApplicationInsights* to enable logging and monitoring.   

    "ApplicationInsights": {
        "LogLevel": {
        "Default": "Trace",
        "System": "Trace",
        "Microsoft": "Trace",
        "Microsoft.Hosting.Lifetime": "Information",
        "System.Net.Http.HttpClient": "Trace"
        }
    }
    "Logging": {
    "LogLevel": {
      "Default": "Trace",
      "System": "Trace",
      "Microsoft": "Trace",
      "Microsoft.Hosting.Lifetime": "Information",
      "System.Net.Http.HttpClient": "Trace"
    }
  }


## Pipeline Variables
    
### Variable Groups

***pipelineVariables***

    - acrConatinerRegistry
    - acrContainerRepositoryHarvester
    - acrName
    - sonarCloudOrganization
    - sonarProjectKeyHarvester
    - sonarProjectNameHarvester

***azureVariables-[dev/test/sandbox/...]***

    - aksNamespace
    - blobStorageUri
    - keyVaultUri
    - serviceBusHostName

***harvesterServiceVariables-[dev/test/sandbox/...]***

    - containerRepostitoryFullPath
    - jnccSchedule
    - medinSchedule
    - serviceAccountHarvester