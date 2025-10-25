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

output "storage_account_name" {
  description = "The name of the generated storage account."
  value       = azurerm_storage_account.sa.name
}

output "function_app_name" {
  description = "The name of the Function App."
  value       = azurerm_function_app_flex_consumption.function_app.name
}
