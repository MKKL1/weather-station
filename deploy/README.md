Azure Weather Station Terraform ModuleThis module provisions a complete backend for a "Weather Station" IoT application on Azure.It creates the following core resources:Resource Group: A container for all resources.IoT Hub: For device ingestion and management.Device Provisioning Service (DPS): To automate device provisioning, linked to the IoT Hub.Storage Account: Used by the Function App. A unique name is generated if not provided.Cosmos DB Account: A serverless SQL API account with a primary database and three containers (...-container, ...-views, ...-leases).Service Plan: A Flex Consumption plan for the Function App.Function App: A .NET 8 Isolated Function App configured with connection strings for the IoT Hub and Cosmos DB.DPS Enrollment Group: (Via a local submodule) A default enrollment group for the DPS.UsageHere is a basic example of how to use this module.Note: You must have the dps_enrollment_group module available at the path specified in main.tf (e.g., ./modules/dps_enrollment_group).terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>4.0" # Loosened version for reusability
    }
    azapi = {
      source  = "Azure/azapi"
      version = "~>2.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~>3.0"
    }
  }
}

provider "azurerm" {
  features {}
  # The subscription ID should be configured by the user,
  # not the module. This can be done via env vars or other auth methods.
}

provider "azapi" {}

module "weather_station" {
  source = "../weather-station-module" # Assumes module is in a parent directory

  # This is the only required variable
  subscription_id = "your-azure-subscription-id"

  # --- Optional Overrides ---
  project_name = "my-weather-app"
  environment  = "prod"
  location     = "East US"

  # You can override the failover location
  cosmosdb_failover_location = "Central US"

  # You can force a specific storage account name
  # storage_account_name = "myuniquesanameprod"
}

output "iot_hub_hostname" {
  description = "Hostname of the deployed IoT Hub"
  value       = module.weather_station.iot_hub_hostname
}

output "dps_id_scope" {
  description = "ID Scope for the deployed DPS"
  value       = module.weather_station.dps_id_scope
}
Provider RequirementsNameVersionhashicorp/azurerm~>4.0Azure/azapi~>2.0hashicorp/random~>3.0InputsNameDescriptionTypeDefaultRequiredsubscription_idSubscription ID for Azure resourcesstringn/ayesproject_nameName of the projectstring"weather-station"nolocationAzure regionstring"West Europe"noenvironmentEnvironment namestring"dev"nocosmosdb_failover_locationSecondary failover location for Cosmos DB. Must be a valid Azure region.string"Germany West Central"nostorage_account_nameThe name for the storage account. Must be globally unique, 3-24 chars, lowercase alphanumeric. If left empty, a unique name will be generated.string""nocosmosdb_name_prefixPrefix for the Cosmos DB account name.string"cosmos"nofunction_app_container_nameName for the storage container for the function app. Must be lowercase, 3-63 chars, alphanumeric, and hyphens.string"function-app-files"noOutputsNameDescriptionresource_group_nameResource Group nameiot_hub_nameIoT Hub nameiot_hub_hostnameIoT Hub hostnamedps_nameDevice Provisioning Service namedps_endpointDPS global endpointdps_id_scopeDPS ID Scopeservice_connection_stringService connection string (Sensitive)cosmos_primary_connection_stringPrimary connection string for Cosmos DB (Sensitive)cosmos_database_nameCosmos DB database namecosmos_container_nameCosmos DB container namestorage_account_nameThe name of the generated storage account.function_app_nameThe name of the Function App.