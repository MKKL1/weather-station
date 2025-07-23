terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=4.37.0"
    }
  }
}

provider "azurerm" {
  subscription_id = var.subscription_id
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = var.location

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "azurerm_iothub" "iothub" {
  name                = "iot-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  event_hub_partition_count = 2

  sku {
    name     = "S1"
    capacity = "1"
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
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

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
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