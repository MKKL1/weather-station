# --- Core Infrastructure ---
output "resource_group_name" {
  description = "Resource Group name"
  value       = azurerm_resource_group.project_scope.name
}

output "location" {
  description = "The Azure Region where resources were created"
  value       = azurerm_resource_group.project_scope.location
}

output "apim_gateway_url" {
  description = "The HTTP endpoint for the API Gateway (The entry point for devices)"
  value       = azurerm_api_management.api_gateway.gateway_url
}

output "apim_name" {
  description = "The name of the API Management service"
  value       = azurerm_api_management.api_gateway.name
}

output "function_app_main_name" {
  description = "The name of the Main Telemetry Function App"
  value       = azurerm_function_app_flex_consumption.main_function.name
}

output "function_app_provisioning_name" {
  description = "The name of the Provisioning Function App"
  value       = azurerm_function_app_flex_consumption.provisioning_function.name
}

output "function_app_main_hostname" {
  description = "Default hostname of the main function (mostly for backend debugging, as APIM sits in front)"
  value       = azurerm_function_app_flex_consumption.main_function.default_hostname
}
output "deployment_storage_account_name" {
  description = "Storage account where function zip packages must be uploaded"
  value       = azurerm_storage_account.backend_storage.name
}

output "deployment_container_main" {
  description = "Container for Main Function App artifacts"
  value       = azurerm_storage_container.deployment_packages_main.name
}

output "deployment_container_provisioning" {
  description = "Container for Provisioning Function App artifacts"
  value       = azurerm_storage_container.deployment_packages_provisioning.name
}

output "static_web_app_deployment_token" {
  description = "Deployment token for pushing JWKS files to the Static Web App"
  value       = azurerm_static_web_app.public_keys_host.api_key
  sensitive   = true
}

output "jwks_url" {
  value       = "https://${azurerm_static_web_app.public_keys_host.default_host_name}/.well-known/jwks.json"
  description = "Full URL to the JWKS endpoint"
}

output "jwks_host" {
  value       = azurerm_static_web_app.public_keys_host.default_host_name
  description = "Hostname of the JWKS server"
}

output "cosmos_primary_connection_string" {
  description = "Primary connection string for Cosmos DB"
  value       = azurerm_cosmosdb_account.iot_database_account.primary_sql_connection_string
  sensitive   = true
}

output "key_vault_name" {
  description = "Name of the Key Vault"
  value       = azurerm_key_vault.secrets_vault.name
}

output "app_insights_connection_string" {
  description = "Connection string for App Insights (useful for local debug config)"
  value       = azurerm_application_insights.backend_tracing.connection_string
  sensitive   = true
}