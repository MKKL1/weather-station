resource "azurerm_service_plan" "main_plan" {
  name                = "asp-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.project_scope.name
  location            = azurerm_resource_group.project_scope.location
  os_type             = "Linux"
  sku_name            = "FC1"
  tags                = var.tags
}

resource "azurerm_function_app_flex_consumption" "main_function" {
  name                = "fn-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.project_scope.name
  location            = azurerm_resource_group.project_scope.location
  service_plan_id     = azurerm_service_plan.main_plan.id

  storage_container_endpoint  = "${azurerm_storage_account.backend_storage.primary_blob_endpoint}${azurerm_storage_container.deployment_packages_main.name}"
  storage_container_type      = "blobContainer"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.backend_storage.primary_access_key

  runtime_name           = "dotnet-isolated"
  runtime_version        = "8.0"
  maximum_instance_count = 40
  instance_memory_in_mb  = 512
  site_config {}

  identity {
    type = "SystemAssigned"
  }

  auth_settings {
    enabled                       = true
    unauthenticated_client_action = "RedirectToLoginPage"
    default_provider              = "AzureActiveDirectory"
    active_directory {
      client_id = azuread_application.function_app_main_auth.client_id
      allowed_audiences = [
        azuread_application.function_app_main_auth.client_id,
        "api://${azuread_application.function_app_main_auth.client_id}"
      ]
    }
  }

  app_settings = {
    AzureWebJobsStorage                        = azurerm_storage_account.backend_storage.primary_connection_string
    COSMOS_CONNECTION                          = azurerm_cosmosdb_account.iot_database_account.primary_sql_connection_string
    COSMOS_DATABASE                            = azurerm_cosmosdb_sql_database.telemetry_db.name
    COSMOS_CONTAINER                           = azurerm_cosmosdb_sql_container.telemetry_container.name
    COSMOS_VIEWS_CONTAINER                     = azurerm_cosmosdb_sql_container.telemetry_views.name
    COSMOS_TELEMETRY_CONTAINER                 = azurerm_cosmosdb_sql_container.telemetry_raw.name
    APPLICATIONINSIGHTS_CONNECTION_STRING      = azurerm_application_insights.backend_tracing.connection_string
    ApplicationInsightsAgent_EXTENSION_VERSION = "~3"
  }

  tags = var.tags
}

resource "azurerm_service_plan" "provisioning_plan" {
  name                = "asp-${var.project_name}-${var.environment}-provisioning"
  resource_group_name = azurerm_resource_group.project_scope.name
  location            = azurerm_resource_group.project_scope.location
  os_type             = "Linux"
  sku_name            = "FC1"
  tags                = var.tags
}

resource "azurerm_function_app_flex_consumption" "provisioning_function" {
  name                = "fn-${var.project_name}-${var.environment}-provisioning"
  resource_group_name = azurerm_resource_group.project_scope.name
  location            = azurerm_resource_group.project_scope.location
  service_plan_id     = azurerm_service_plan.provisioning_plan.id

  storage_container_endpoint  = "${azurerm_storage_account.backend_storage.primary_blob_endpoint}${azurerm_storage_container.deployment_packages_provisioning.name}"
  storage_container_type      = "blobContainer"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.backend_storage.primary_access_key

  runtime_name           = "custom"
  runtime_version        = "1.0"
  maximum_instance_count = 40
  instance_memory_in_mb  = 512
  site_config {}

  identity {
    type = "SystemAssigned"
  }

  auth_settings {
    enabled                       = true
    unauthenticated_client_action = "RedirectToLoginPage"
    default_provider              = "AzureActiveDirectory"
    active_directory {
      client_id = azuread_application.function_app_auth.client_id
      allowed_audiences = [
        azuread_application.function_app_auth.client_id,
        "api://${azuread_application.function_app_auth.client_id}"
      ]
    }
  }

  app_settings = {
    AzureWebJobsStorage                        = azurerm_storage_account.backend_storage.primary_connection_string
    COSMOS_CONNECTION                          = azurerm_cosmosdb_account.iot_database_account.primary_sql_connection_string
    COSMOS_DATABASE                            = azurerm_cosmosdb_sql_database.telemetry_db.name
    COSMOS_CONTAINER                           = azurerm_cosmosdb_sql_container.telemetry_container.name
    JWT_ISSUER                                 = "https://${azurerm_static_web_app.public_keys_host.default_host_name}/device"
    WEBSITE_AUTH_AAD_ALLOWED_TENANTS           = data.azurerm_client_config.current.tenant_id
    APPLICATIONINSIGHTS_CONNECTION_STRING      = azurerm_application_insights.backend_tracing.connection_string
    ApplicationInsightsAgent_EXTENSION_VERSION = "~3"

    ACCESS_TOKEN_PRIVATE_KEY_B64 = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.token_signing_key.id})"
  }

  tags = var.tags
}

resource "azurerm_key_vault_access_policy" "provisioning_app_access" {
  key_vault_id = azurerm_key_vault.secrets_vault.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_function_app_flex_consumption.provisioning_function.identity[0].principal_id

  secret_permissions = ["Get"]
}