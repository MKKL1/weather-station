terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">=4.49.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = ">=2.5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~>3.0"
    }
  }
}

provider "azurerm" {
  features {}
  # subscription_id = "your-subscription-id-here"
}

provider "azapi" {}

module "weather_station_dev" {
  source = "../"
  subscription_id = "your-azure-subscription-id"
  project_name = "weather-app"
  environment  = "dev"
  location     = "West Europe"
}

output "dev_iot_hub_hostname" {
  description = "Hostname of the DEV IoT Hub"
  value       = module.weather_station_dev.iot_hub_hostname
}

output "dev_dps_id_scope" {
  description = "ID Scope for the DEV DPS"
  value       = module.weather_station_dev.dps_id_scope
}

output "prod_iot_hub_hostname" {
  description = "Hostname of the PROD IoT Hub"
  value       = module.weather_station_prod.iot_hub_hostname
}
