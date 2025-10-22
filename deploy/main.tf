terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=4.49.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "=2.5.0"
    }
  }
}

provider "azurerm" {
  subscription_id = var.subscription_id
  features {}
}

provider "azapi" {}

resource "azurerm_resource_group" "rg" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = var.location
}

resource "azurerm_iothub" "iothub" {
  name                = "iot-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  event_hub_partition_count = 2

  sku {
    name     = "F1"
    capacity = "1"
  }
}

resource "azurerm_iothub_shared_access_policy" "hub_access_policy" {
  name                = "terraform-policy"
  resource_group_name = azurerm_resource_group.rg.name
  iothub_name         = azurerm_iothub.iothub.name

  registry_read   = true
  registry_write  = true
  service_connect = true
}

resource "azurerm_iothub_dps" "dps" {
  name                = "dps-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  sku {
    name     = "S1"
    capacity = "1"
  }

  linked_hub {
    connection_string = azurerm_iothub_shared_access_policy.hub_access_policy.primary_connection_string
    location          = azurerm_resource_group.rg.location
  }
}

resource "azurerm_storage_account" "sa" {
  name                     = "weatherstationdevappsa" #unique name required
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_service_plan" "flex_asp" {
  name                = "flex_asp-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "FC1"
}

resource "azurerm_cosmosdb_account" "this" {
  name                = "cosmos2-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = "Germany West Central" #In my case, West Europe is not available
    failover_priority = 0
  }

  capabilities {
    name = "EnableServerless"
  }
}

resource "azurerm_cosmosdb_sql_database" "this" {
  name                = "${var.project_name}-db"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.this.name
}

resource "azurerm_cosmosdb_sql_container" "this" {
  name                = "${var.project_name}-container"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.this.name
  database_name       = azurerm_cosmosdb_sql_database.this.name

  partition_key_paths = ["/deviceId"]
}

resource "azurerm_cosmosdb_sql_container" "views" {
  name                = "${var.project_name}-views"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.this.name
  database_name       = azurerm_cosmosdb_sql_database.this.name

  partition_key_paths = ["/deviceId"]
}

resource "azurerm_cosmosdb_sql_container" "leases" {
  name                = "${var.project_name}-leases"
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.this.name
  database_name       = azurerm_cosmosdb_sql_database.this.name

  partition_key_paths = ["/id"]
}

resource "azurerm_storage_container" "function_app_container" {
  name                  = "functionappcontainerheavyweatherdev" # Must be lowercase, 3-63 characters, alphanumeric, and hyphens
  storage_account_id    = azurerm_storage_account.sa.id # Uses full Resource Manager ID
  container_access_type = "private" # Use "private" for security; adjust if needed
}

# Function App with Flex Consumption Plan
resource "azurerm_function_app_flex_consumption" "function_app" {
  name                = "fn-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.location
  service_plan_id     = azurerm_service_plan.flex_asp.id

  storage_container_endpoint  = "${azurerm_storage_account.sa.primary_blob_endpoint}${azurerm_storage_container.function_app_container.name}"
  storage_container_type      = "blobContainer"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.sa.primary_access_key

  runtime_name    = "dotnet-isolated"
  runtime_version = "8.0"

  # Optional: Configure instance limits and memory
  maximum_instance_count = 40
  instance_memory_in_mb  = 512

  site_config {} # Required, even if empty

  app_settings = {
    AzureWebJobsStorage      = azurerm_storage_account.sa.primary_connection_string

    EH_CONN_STRING = "Endpoint=${azurerm_iothub.iothub.event_hub_events_endpoint};SharedAccessKeyName=${azurerm_iothub_shared_access_policy.hub_access_policy.name};SharedAccessKey=${azurerm_iothub_shared_access_policy.hub_access_policy.primary_key};EntityPath=${azurerm_iothub.iothub.event_hub_events_path}"
    EH_NAME        = azurerm_iothub.iothub.event_hub_events_path

    COSMOS_CONNECTION      = azurerm_cosmosdb_account.this.primary_sql_connection_string
    COSMOS_DATABASE        = azurerm_cosmosdb_sql_database.this.name
    COSMOS_CONTAINER       = azurerm_cosmosdb_sql_container.this.name
    COSMOS_VIEWS_CONTAINER = azurerm_cosmosdb_sql_container.views.name
    COSMOS_LEASE_CONTAINER = azurerm_cosmosdb_sql_container.leases.name
  }

  depends_on = [azurerm_storage_container.function_app_container]
}

module "enrollment-group" {
  source            = "./modules/dps_enrollment_group"
  subscription_id   = var.subscription_id
  resource-group    = azurerm_resource_group.rg.name
  iothub            = azurerm_iothub.iothub.name
  iothub-hostname   = azurerm_iothub.iothub.hostname
  dps               = azurerm_iothub_dps.dps.name
  enrollment-name   = "${azurerm_iothub_dps.dps.name}-main"
  initial-twin-tags = "[]"

  depends_on = [azurerm_iothub_dps.dps]
}






output "resource_group_name" {
  description = "Resource Group name"
  value       = azurerm_resource_group.rg.name
}

output "iot_hub_name" {
  description = "IoT Hub name"
  value       = azurerm_iothub.iothub.name
}

output "iot_hub_hostname" {
  description = "IoT Hub hostname"
  value       = azurerm_iothub.iothub.hostname
}

output "dps_name" {
  description = "Device Provisioning Service name"
  value       = azurerm_iothub_dps.dps.name
}

output "dps_endpoint" {
  description = "DPS global endpoint"
  value       = "global.azure-devices-provisioning.net"
}
output "dps_id_scope" {
  description = "DPS ID Scope"
  value       = azurerm_iothub_dps.dps.id_scope
}

output "service_connection_string" {
  description = "Service connection string"
  value       = azurerm_iothub_shared_access_policy.hub_access_policy.primary_connection_string
  sensitive   = true
}

output "cosmos_primary_connection_string" {
  description = "Primary connection string for Cosmos DB"
  value       = azurerm_cosmosdb_account.this.primary_sql_connection_string
  sensitive   = true
}

output "cosmos_database_name" {
  description = "Cosmos DB database name"
  value       = azurerm_cosmosdb_sql_database.this.name
}

output "cosmos_container_name" {
  description = "Cosmos DB container name"
  value       = azurerm_cosmosdb_sql_container.this.name
}